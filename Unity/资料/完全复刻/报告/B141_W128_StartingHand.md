# B141_W84 · `B141-STARTHAND`「教程起手 `_startinghand` 占位卡」——只读核对报告

- 批次 / 会话：`B141` / 第一百二十八会话（W128）
- 上游：`报告/B141_W128_BL37Discard.md` §六·2（`W58` 的 ❓）
- 结论档位：**✅ 无需改**（现状与原版净效果等价）＋ **🟠 另有 1 条同链真缺陷**（3 张起手卡认不出）＋ **❓ 占位卡的原版消费者本地无引用**

---

## 〇、一句话结论

两张 `_startinghand` 占位卡**不在我们任何数据源里**，**2025-07-30 与 2026（1.35.0 APK）两份原版 `TutorialStage` 的起手列表也都不引用它们**
⇒ **教程起手装配不需要处理占位卡**；若将来数据真把它带出来，缺的只有「手牌上下文 `ca 100` 造卡」这一支（`ca 160` 已于本会话 `B141_W67` 落地）。

---

## 一、现状：占位卡在哪、在哪都找不到

占位卡两张（`资料/普查产出_1018/allcards_1350/cards_raw.json`，1,351 张 SO）：

| pid | m_Name | uniqueId |
|---|---|---|
| `-189149361816600990` | `T1 BlackLegion-Abaddons Chosen_startinghand` | `f3df0cfc57695654cb31a72b65470522` |
| `-6834136291891806766` | `Z-NC SpaceWolves-Tyrnak and Fenrir_startinghand` | `8f0302f1603cc954392d90d5dabd22d5` |

现读命中表（逐项现扫）：

| 数据源 | 规模 | 占位卡命中 |
|---|---|---|
| `Resources/cards_engine.json`（引擎实际加载的池，`Data/CardDatabase.cs:17-18`） | v49 / 1,142 张 | **0**（整份 JSON 搜 `startinghand` = 0） |
| `Resources/tutorial_decks.json` | 12 副教程牌 | **0** |
| `Resources/prebuilt_decks*.json`（三份） | —— | **0** |
| `Resources/tutorial_stages.json`（= 2025-07-30 的 6 个 TutorialStage SO） | 6 关 | **0** |
| 2026 APK `tutorialso_assets_all`（6 关 SO，内存解析） | 7 个 MB | **0**（全 typetree 递归扫 pathID，且搜两个 uniqueId 字符串） |

**已扫的 2026 包（内存解析，未落盘）**：`tutorialso` · `prebuiltdecks` · `duplicateassetisolation` · `duplicateassetisolationso` · `draftpacks` · `cosmeticsso` · `menus` · `mainmenualwaysloaded` · `boosterpacks` · `deckselectionbuttons` · `staticgeneralassets` · `tutorialwarlordchats` —— 两个 pathID 与两个 uniqueId **全 0 命中**。
⇒ 这两张 SO 在本地是**孤儿**：只存在于 cards 包自身。2026 教程牌组的 `cardLibraryIds` 是**字符串 id 列表**（如 `SAU3`、`847acecf…`），也没有它们。

---

## 二、原版语义对照（含对 `W58` 两处订正的证据）

1. **2026 的 6 关 `TutorialStage` 与 2025 快照逐 stage 全等**：用 UnityPy 读
   `d:/4/_tmp_view/apk135/UnityDataAssetPack.apk` 内 `assets/aa/Android/tutorialso_assets_all.bundle`，
   取 `playerStartingTroopsInHand` / `enemyStartingTroopsInHand` 的 `m_PathID` 序列，与
   `tutorial_stages.json` 的 `pid` 列表比对 —— **6/6 `True`**（S1/S2 两列表均空；S3 4/3、S4 3/4、S5 3/4、S6 3/4）。
2. ⇒ `W58` §六·2 的强推断「消费者 = `TutorialStage.{player,enemy}StartingTroopsInHand`」**被反证推翻**：两个 build 的该字段都**不引用**占位卡。
3. ⇒ `W58` 的另一句「allcards(2026) 与 assets_full(2025) **pathID 不互通**」**不成立**：
   把 2025 关卡 SO 里 **171 个带名字的 pid** 拿去 2026 `cards_raw` 按 `path_id` 反查 ⇒ **76 个命中、名字不符 0 个**；
   加上第 1 条的「起手 pid 逐字相同」，说明**卡 pathID 跨 build 稳定**（当年 0 命中的真因是 `assets_full` 里**根本没有 allcards 包**，不是 id 变了）。
4. **仍未解的一点**：占位卡在本地**找不到任何消费者**（cards 包内也没有别的 SO 通过 guid/pathID 引用它们；教程牌组的 `cardLibraryIds` 也没有）。
   原版用法**未证**（可能在被远端/服务端裁掉的卡库资产里）。但**不影响我们**，见三、五。

---

## 三、我们的教程起手装配链（现读）

- `RuleCore.cs:343-349`：`tutorial != null` 时取 `st`，`PoolLookup(ctx)`，两次 `TutorialRules.SetupInitialHand(...)`，再 `SetupStartingTroops` / 初始法力。
- `TutorialScript.cs:1598-1626`：列表逐项 `Resolve(ourId)` → `ps.Deck.Add(ctx.NewInstance(d))`（追加到末尾 = 我们的牌库顶）→ 逐张 `RuleCore.Draw`；
  **认不出 ⇒ 打警告 + 跳过（⛔ 不猜）**。
- `TutorialScript.cs:1669-1681` `PoolLookup`：**只按 `CardDef.Id` 精确匹配**，不按名字模糊找。
- 教程局**不跑换牌阶段**（`RuleCore.cs:335-340` 的注释与判据链）⇒ 起手只来自这两个列表。
- ⇒ 我们是**直接把真卡放进手牌**，与原版「占位卡在手 → 回合开始弃自己 → 造真卡」的**净效果相同**（只差一拍：原版真卡在首个回合开始时才出现；教程前两关列表为空、S3–S6 首回合脚本不依赖这一拍）。
- ⚠️ **现读 3 处未解条目**（pid 有、`ourId` 空）：S3 玩家 `[0]`、S6 玩家 `[1]`/`[2]` ⇒ 现在分别**少 1 / 2 张起手**（`SetupInitialHand` 会出声）。这三张在 2026 `cards_raw` 里都能解，见六·1。

---

## 四、`ca 160` / `ca 100` 在教程局的可达性

- 手牌触发路：`BroadcastTurnStartHandHops`（`EffectResolver.cs:19186-19214`，`:19204` 发 `TurnStart` 给双方手牌）
  → `FireHandAbilityTrigger`（`:19029`）→ 文本 op 为空时走 `DispatchHandAbilityRows`（`:19101`）。
- **`ca 160 discardFromHand`（`ta = 20 self`）：已实现** ——
  `CardAbilityDispatch_B116.cs:381-398` 判 `Owned`，`:449` 执行（复用 `DiscardHandCardCore`，本会话 `B141_W67`）。
  主路（棋盘/战术卡）那一支在 `EffectResolver.cs:18790`（`B97_W5`）。
- **`ca 100 createCardInHand` 在【手牌上下文】没有执行器** —— `CardAbilityDispatch_B116.cs:395-397` 判 `Unsupported`（出声、不结算）；
  它的执行体 `EnchantmentExecutor_B115.cs:1547-1580` **要 `UnitState` carrier**（附魔），手牌不是 `UnitState`。
- ⇒ 假设占位卡真进了手牌：`160` 会**先把它自己弃掉**，`100` **只出声不造卡** ⇒ 占位卡被吃掉、真卡不出现（比现状更糟）。
  但**今天零可达**（占位卡不在池、也不在任何数据源里）。
- 施工点（**仅当**数据带出占位卡时才需要）：① `cards_engine` 收占位卡（`ourId` = 其 uniqueId 那一档）；
  ② 在 `CardAbilityDispatch_B116.DispatchHandAbilityRows` 补手牌上下文的 `ca 100`（取牌参数来自行 `summonCriteria.unitsToSummon`，
  造卡落点复用 `RuleCore.AddCardToHandWithinLimit`，⛔ 不要另写一份造卡规则）。

---

## 五、结论

- **✅ 无需改**：现状（起手直接把真卡放进手牌）与原版净效果等价，且两个 build 的原版数据都不引用占位卡。
- **🟠 有一条同链真缺陷（独立于占位卡）**：S3 玩家、S6 玩家共 **3 张**起手卡认不出 ⇒ 起手少 3 张。施工点见六·1。
- **❓ 占位卡的原版消费者未证**（本地无引用；本地 90 个包里，2026 教程牌组 `cardLibraryIds` 是 id 字符串、也没有它们）。
- 本笔**没有改动任何文件**（只写本报告）：没有可断言的**新**行为，且占位卡不可达 ⇒ 建断言就是同义反复。

---

## 六、顺手发现（请调度台分流）

1. **3 个 no-name 起手 pid 现在可解**（2026 `cards_raw` 反查 + 池内已存在，现读池 `cards_engine` v49）：

| pid | 2026 SO 名 | 池 id |
|---|---|---|
| `1696504206920290120` | `34 Sautekh-Monolith` | `SAU34` Monolith |
| `8270355771166178363` | `29 Leviathan-Exocrine` | `TL27` Exocrine |
| `8906429412044970690` | `32 Leviathan-Tyrannofex` | `TL30` Tyrannofex |

   另 5 个 no-name pid 同样可解（都不是起手卡）：`-124800208381561086`→`GOF2` Makari · `4406439244507976715`→`ASH7` Veteran Guardian ·
   `7386907376838221809`→`SAU4` Gauss Warrior · `8095124868697240450`→`141c5ed6…` Unhallowed Gifts · `8223774895360888806`→`UM19` Aggressor Sergeant。
   建议：`工具/gen_tutorial_stages.py` 的「pid → 名字」回退链加一档「查 2026 allcards `cards_raw.json`」，再按卡名/uniqueId 映射到池 id
   （⛔ 别手改产物；tutorial 变体那两条要先按 `ocrNameFixes` 的口径裁定）。
2. `W58` 报告 §六·2 的两句需就地订正（反证见本报告二·2 / 二·3）。
3. `B97W5_RowsAndDedupe.cs:24-25` 措辞「BL37 的重复 SO」的补丁 `W58` 已写好但**未落**（该文件不在其白名单）—— 本笔也未动。

---

## 七、验证口径 / 出处

- **只读**：未改源码/资产/账/正本/活状态表/所有权表；未跑 Unity；未动 git。2026 包**内存解析（UnityPy 1.24.2），未落盘**。
- 2026 数据：`d:/4/_tmp_view/apk135/UnityDataAssetPack.apk` → `assets/aa/Android/{tutorialso,prebuiltdecks,…}.bundle`。
- 2025 数据：`Unity/MyGame/Assets/RuleEngine/Resources/tutorial_stages.json`（`source` = `assets_full/bundle_tutorialso_assets_all` 6 个 SO）。
- 池：`Resources/cards_engine.json` v49 / 1,142 张（引擎加载路径 `Data/CardDatabase.cs:17-18`）；SO 真值：`资料/普查产出_1018/allcards_1350/cards_raw.json` / 1,351 张。
- 装配链：`RuleCore.cs:343-349` · `TutorialScript.cs:1598-1626` · `:1669-1681`。
- 手牌触发 / 行级派发：`EffectResolver.cs:19029·19101·19186-19214` · `CardAbilityDispatch_B116.cs:381-398·449` · `EnchantmentExecutor_B115.cs:1547-1580` · `EffectResolver.cs:18790`。
