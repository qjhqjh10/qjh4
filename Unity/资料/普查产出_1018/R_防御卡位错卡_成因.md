# R · 防御卡位错卡（ASH67）成因普查 · 2026-10-18

> 只读代理产出（无写文件工具 ⇒ 由主对话原样落盘）。

## 1. 一句话结论

**不是 K06，也不是建组时挑错卡**：这副「复仇者之刃」是 **2026-10-12 12:14** 由
`DeckRuntime.PlayerDeckForDemo` 写进存档的，当时 `ASH67` = `Aspect Shrine`（defence，cost 1），完全正确；
**2026-10-13/14「卡牌 id 整族换原版真值」（commit `601005b`，2026-10-14 00:09）把 `ASH67` 改指 `Hornet`（unit）**，
而 `LegacyCardIds` 只覆盖 4 个自造 token、K06 的 `poolTrueIds` 只记录 10-18 那 114 条，
都没覆盖这次 **955 张**的换号 ⇒ 旧真 id 指到了另一张卡。

## 2. ASH67 的前世今生（证据链）

| 代际 | ASH67 | 证据 |
|---|---|---|
| 旧代（≤2026-10-13） | `Aspect Shrine` · SaimHann · **defence** · cost 1 | `_tmp_view/cardid_bak_1013/cards_engine.json`（mtime 10-13 23:26）+ 同名 `card_ids.json` 的 `mapping["ASH67"]="Aspect Shrine"` |
| 新代（2026-10-14 起） | `Hornet`（黄蜂）· SaimHann · **unit** · cost 5 · ranged 5 · rare | 现池 `RuleEngine/Resources/cards_engine.json`；`_tmp_view/p1_1014/bak/cards_engine.json`（10-14 23:40）已同 |

- `Hornet` 的旧号是 `ASH78`；`Aspect Shrine` 的现号是 **`ASH55`**（`mapping["ASH55"]="Aspect Shrine"`）。
- 换号点：`git show 601005b^` vs `601005b` 的 `cards_engine.json` **955/1126 张 id 变了**（12 阵营全中，SaimHann 88 张），
  其中 `Hornet ASH78→ASH67`、`Aspect Shrine ASH67→ASH55`。同日 `card_ids.json` 的 `mapping["ASH67"]` 从 `Aspect Shrine` 变成 `Hornet`。
- **K06（10-18）不是成因**：`_tmp_view/backup_1018/k06/cards_engine.json.BEFORE` 里 `ASH67` 已经是 `Hornet`；
  K06 实测只改 **114 条，全是 UM_/SW/DA**，`ASH*` 0 条。`poolTrueIds["ASH67"]="ASH67"` 只记录「10-18 前池 id → 真 id」，
  对 10-14 换号天然是盲的。
- 谁写的：`Deck/DeckRuntime.cs:8450-8470` 的 `PlayerDeckForDemo`，`DeckRuntime.cs:1263-1265` 调用；
  防御卡赋值在 `DeckRuntime.cs:8461`（池里第一张同阵营 `Type=="defence"`）。
  **复算坐实**：拿 10-13 池跑同一算法，30 张 `CardIds` 与 `DefensiveId="ASH67"` 与存档逐字相同。
  其他赋值落点均排除：`DeckEditorState.TryAdd`（手点防御卡，:289）· `PrebuiltDecks.ToPlayerDeck`（:237，现值为 `ASH56`）·
  `DeckLibrary.CommitCurrent`（:269，纯拷贝）· `LegacyCardIds.MigrateDeck`（:104，ASH67 不在表里）。

## 3. 影响面（只读扫描）

- 存档：主 profile `…/com.unity.template.urp-blank/WarpforgeDecks.json` 2 套中 1 套坏；
  副 profile `…/com_unity_template_urp-blank/WarpforgeDecks.json` 唯一 1 套坏（内容逐字相同）。**2 处**。
- 回放：`…/WarpforgeReplays/` 4 份中 3 份（10-11、10-13、10-15）的 `start.hostDeckJson` 是同一副、`DefensiveId="ASH67"`。**3 处**。
- 合计 **5 个卡组实例**。
- **不是孤例，是一类**：同一副 30 张里 **13 个非 token 真 id 全部被 10-14 换号改指了别的卡**
  （`ASH32` Dire Avenger Exarch→Wraithlord、`ASH19` Dire Avenger→Witchblade Warlock、`ASH7` Guardian Defender→Veteran Guardian …）。
  只跑现有 4-token 迁移后，该副仍有 3 处超同名上限（`ASH34`×2 legendary、`ASH79`×3 legendary、`ASH80`×3 epic）
  ⇒ **就算清掉防御卡位，仍会以 `CopyLimitExceeded` 判不合法**，且实际打的是另一套牌。
  另：`ASH_Farseer→ASH80` 与旧 `ASH80`、`ASH_Orian→ASH79` 与旧 `ASH79` 发生同 id 合并，正是两代 id 混用的直接后果。
- 预组侧无坏值：现 `prebuilt_decks.json` 103 副的 `defensiveId` **103/103 都是 defence**（SaimHann 统一 `ASH56`）。

## 4. 该怎么修（建议，未动手）

1. **首选：新增「池 id 代际迁移表」，读时只迁内存**（沿用 `LegacyCardIds` 的既有形态；⛔ 不要按卡名运行时匹配，表必须离线逐条核）。
   至少覆盖本副 18 条：`ASH67→ASH55`；`ASH32→ASH63`、`ASH19→ASH62`、`ASH34→ASH22`、`ASH7→ASH4`、`ASH79→ASH69`、
   `ASH20→ASH15`、`ASH9→ASH6`、`ASH35→ASH28`、`ASH26→ASH17`、`ASH8→ASH5`、`ASH27→ASH23`、`ASH15→ASH11`、`ASH80→ASH70`，
   加 4 个 token（`ASH_Autarch→ASH83`、`ASH_Death_Spinner_Warp_Spider→ASH77`、`ASH_Farseer→ASH80`、`ASH_Orian_Laratharjos→ASH79`）。
   🔴 **必须一次性并发替换**（`ASH20→ASH15` 与 `ASH15→ASH11` 成链，顺序替换会静默改成第三张卡）。
   入口：`DeckStore.LoadAll`、`ReplayStore.Load`（回放只改内存）。
   验证：该 18 条映射跑完，30 张 = 17 种、全 SaimHann、同名上限全过、防御位 `ASH55 Aspect Shrine/defence`，整副恢复合法。
2. **代际歧义**：`DefensiveId` 无歧义——当前 id 不是 defence、而旧代同 id 是 defence ⇒ 按旧代迁；查不到就清空
   （`DeckRules.Validate` ③ 允许无防御卡），可立即止血让 `Practice Deck` 恢复可点。
   `CardIds` 有歧义（10-14 后新建的卡组不能套旧代表）⇒ 建议给存档加代际版本位，或整副「双解验证」
   （新代解不合法、旧代解合法才迁）。**只补防御位不够**。
3. **建组侧**：`PlayerDeckForDemo` 的防御卡选择改成与 `工具/gen_prebuilt_decks.py:pick_defensive` 同一规则
   （本阵营 defence 里 cost 最低 → id 最小），并在落库/开局前断言 `Type=="defence"`。
   注意我们的 `CardDef` **没有 `spellType` 字段**，`Type=="defence"` 就是仓库里唯一的等价判据。
4. **扫描闸补一层语义**：`W_旧卡id迁移闸.md` 只查了「id 是否在池中」，漏了「id 在池中但已改指另一张卡」。
   普查脚本应加 `(name, faction, type)` 对照（离线静态表）。
5. **红线**：`prebuilt_decks_full.json` 只读、不改；玩家存档/录像文件不改，迁移只改内存。

## 5. 没查清

- 未跑 Unity、未真机复验；回放 `finalHash` 是否已不可复现未实测，只确认其 `hostDeckJson` 是旧代 id。
- 10-14 换号是否有过「玩家存档迁移」设计：`资料/卡牌id_换真值_落地记录.md` 的「连带影响」只写预组/卡图/语音/断言，
  未提玩家存档；全仓 grep 也未找到该次换号的存档迁移实现。
- **全量 955 条旧→新映射未生成**（超出只读任务；本次只逐条核了这副牌的 18 条）。
- 只扫了本机两个 persistentDataPath profile；其它机器/路径未知。
  `C:/Users/qjh36/AppData/LocalLow/Everguild/Warpforge/PlayerDecks39.dat` 是原版存档，我们代码不读（grep 0 命中），未解析。
