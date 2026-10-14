# U · `AM3` 深查（只读 · 2026-10-15）

> 写手 = 只读深查代理。**没跑 Unity、没动 git、没改任何既有文件**；本件是唯一新建文件。
> 引用我方卡表处一律**现读**（`cards_engine.json` 读于 2026-10-15 00:07）。

## ① 卡名

| 来源 | 读数 |
|---|---|
| 真值快照 `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json` | `uniqueId=AM3` 只有 1 条：**`cardName = "Commissar Denkler"`** · `m_Name = "AstraMilitarum_war_Commissar Denkler"` |
| 官方本地化 `Unity/数据/游戏数据/官方本地化_en+zh-CN.json` | `terms["Card_Name/AM3"] = ["Commissar Denkler","政委登克勒"]`；`terms["Warlord_Lore/AM3"] = ["…Commissar **Elan** Denkler keeps his troops…","政委**艾兰**·登克勒…"]`（**Elan = 名、Denkler = 姓**）；`terms["Warlord_Lore/cc1183…"]`（天赋卡）也写 `Commissar Denkler` |
| PnP 成品卡图 | **我开了图**：`d:/2/Warpforge部队卡片/Astra Militarum/1督军/Warpforge_01_Commissar-Denkler.png` → 卡面白字 **`Commissar Denkler`**（橙字 `Astra Militarum`）。文件名编号 `01` 与该文件是同一张（`card_stats.json` 的 `ocrSrc` 正指着它） |
| 我方卡表 | `name = "Commissar Elan"`，但 **`nameZh = "政委登克勒"`** —— 与官方 `Card_Name/AM3` 的中文**逐字相同** ⇒ **我方中英两半自相矛盾** |
| 我方 `card_stats.json` | `name = "Commissar Elan"`（= **立绘资产名尾段**）、**`ocrName = "Commissar Denkler"`**（= 我们自己 OCR 卡面读出来的） |
| 我方 PnP 派生表（2026-08 做的） | `d:/2/Warpforge部队卡片/卡牌信息权威表_0824.md:131` · `卡牌数据总表.md:228` · `卡牌完整信息库_0824.md:90` —— **三处都写 `Commissar Denkler`** |

### `Elan` 在本地各处逐处命中

| 处 | 命中 |
|---|---|
| `d:/2/新解包资源/assets_full/bundle_astramilitarumcardassets_assets_all/Texture2D/AstraMilitarum_warlord_Commissar Elan.png` + 同名 `Sprite/*.json` | **卡面立绘资产名**写 `Elan` |
| 同包 `AudioClip/VO_AM_Commissar Elan_*.ogg` | **23 个语音片段**全写 `Elan` |
| 同包 `AudioClip/VO_AM_Hektor Thenmann_vsDenkler.ogg` | ⭐ **另一张卡的语音点名「vsDenkler」** ⇒ **游戏里这角色叫 Denkler** |
| `bundle_cosmeticsso_assets_all/MonoBehaviour/Title_AM_warlord_Denkler_1.json` / `_2.json` | 化妆品称号写 `Denkler` |
| `bundle_cosmeticsso_assets_all/MonoBehaviour/Avatar_AM_Warlord_Commissar **Deckler**.json` | 头像 SO：`m_Name = Avatar_AM_Warlord_Commissar Deckler` · **`nameTextReference = "Commissar Deckler"`**（**第三种拼写**，**不是 Elan**） |
| `bundle_uiwarlords_assets_all/Texture2D/UI_Deck_Warlord_Commissar Deckler.png` | 卡组界面督军头像 `Deckler` |
| 官方本地化 | **仅** `Warlord_Lore/AM3` 一处（作**名**）；`Card_Name/*` / `Chat_Messages/*` **0 命中** |
| `assets_full` 24.0 万个 `.json` 的**正文** | `Elan` **只 1 命中**（上面那个 Sprite json 里的资产名）；`Denkler` 2 命中（两条 Title SO） |
| PnP 卡面 · 我方派生表 | **0 命中 `Elan`**（印的是 Denkler） |
| 我方下游（`cards_engine.json` / `card_stats.json` / `voice_lines.json` / `ability_anim_durations.json` / `card_vfx_by_card.json` / `card_index.json` / `texture_index.json` / `enum_names.json` / `说明书/03_卡牌/AstraMilitarum.md`） | 全部是**上面那两族资产名（立绘 / 语音）的下游**，**不构成独立证据** |
| `zh_CN.csv:855` / `zh_cards.json:975` | `Commissar Elan,~,政委·埃兰` —— 这是**我们自己按资产名译的**那份（与官方 `政委登克勒` **不同**） |

### 四族证据（照 `资料/普查产出_1017/W_B20_同族四张卡名.md` 的路子）

| 族 | 读数 | 站谁 |
|---|---|---|
| ① 卡图资产名 | `AstraMilitarum_warlord_Commissar **Elan**` | **Elan** |
| ② 语音 AudioClip | `VO_AM_Commissar **Elan**_*`（23 条） | **Elan**（⚠️ 但同包另有一条 `…_vs**Denkler**.ogg` 反向站 Denkler） |
| ③ 头像 SO + `nameTextReference` | `Avatar_AM_Warlord_Commissar **Deckler**` / ntr `"Commissar Deckler"` | **Denkler**（拼错一个字母，**不是 Elan**） |
| ④ 教程 SO | AM3 **不在**教程督军里（教程只有 `UM46/UM47`） | —— |
| ⑤ **官方显示名** `Card_Name/AM3` | `Commissar Denkler` | **Denkler** |
| ⑥ 真值 `cardName` + `m_Name` 尾段 | `Commissar Denkler` | **Denkler** |

### ⇒ 结论倾向

**卡名应取官方 `Commissar Denkler`（`nameZh` 已是「政委登克勒」，一字不用改）。** 证据强度：**很强**。
理由：① 官方 `Card_Name/AM3`（**玩家实际看到的串**）与真值 `cardName` **两条独立权威**都写 Denkler；
② PnP 印刷面（我亲开图）也写 Denkler；③ 我们**自己**的 OCR（`ocrName`）与三份 PnP 派生表也早就读出 Denkler；
④ `Elan` 只活在**立绘/语音的资产名**里，而 `Warlord_Lore/AM3` 明写 **"Commissar Elan Denkler"** ⇒ `Elan` 是**名**、不是卡名；
⑤ 我方 `nameZh` 早已是官方 Denkler 的中文 ⇒ 只需改英文那一半。
⚠️ `W_B20` 那条「卡图资产名 79/89 胜」的通则**在这里不适用**：那条普查比的是「卡图资产名 vs PnP 写法」，而我们这次拿到了 W_B20 当年**缺的那条判据**（官方 `Card_Name/*` + 真值 `cardName`）。

## ② 数值

| 字段 | 真值快照 | 我们（含来源层） | PnP（我开图亲读） |
|---|---|---|---|
| 近战 | `meleeAttack = 2` | `attack = 2` | 左圆红剑 **2** |
| 远程 | `rangeAttack = 2` | `ranged = 2` | 右圆紫枪 **2** |
| 护甲 | `armour = 0` | **卡表无该字段**；`card_stats.json` `armor = 2` | **卡底无银盾** ⇒ 甲 **0**（对照：`AM38 Baneblade` 甲 2 印成右上角银盾「2」） |
| 生命 | `maxHealth = **35**` | `health = **30**`（来源 = PnP 卡面 OCR 那条链） | 最右绿框 **30** |

- 🔴 **「卡图甲 2」不是卡图读数、是 OCR 误读**：`card_stats.json` 的 AM3 条目是
  `attack 2 / health 30 / **armor 2** / **ranged_attack: null**` ⇒ OCR **把紫圈那个「2」配到了 armor、把远程整列丢了**。
  PnP 卡面**没有任何银盾**（我开图核过，并拿了 `AM38 Baneblade` 当正对照）。⇒ **护甲两侧都 = 0，无争议。**
  ⚠️ `资料/普查产出_第三十一会话/R_卡牌线判据审计.md:19` 把这句写成「`card_stats`/**PnP** 说『护甲 2』」——
  **PnP 那半不成立**（见上），建议就地订正（我没改，只读）。
- **反编译里督军 health / armour 的赋值点**：**有，但是配置驱动的增量，不是改卡面数据。**
  `d:/2/tools/decomp_full/BattleManager__SetupBoardPhase.c:197` → `BattleManager__SetupWarlordsLifeAndEnchantment`
  → `BattleManager__SetupWarlordsLifeAndEnchantment.c:53,72` 调 **`CardScript__ForceChangeMaxHealth(督军, 配置值 + 督军现值, 0)`**，
  唯一守卫是 `if (*(int *)(lVar2 + 0x20) != 0)`（`lVar2` 由 `PlayerDataManager` 静态实例的 `+0x20` 字段解出 ⇒
  **玩家侧配置/战役数据**，配置为 0 时整段跳过）。`CardScript__ForceChangeMaxHealth.c` 只写运行时实例（`+0x24c` / `+0xa4..0xb4`）并刷血条文字。
  ⚠️ **字段名读不出**（反编译无符号）⇒ **明说读不到**；⚠️ 顺手撞到本仓记过的**文件名与内容对不上**：
  `CardData__set_health.c` 里其实是 `RaidRewardSelector_RaidEventMilestoneInfo__set_Level`。
  ⇒ **结论：真值那个 `maxHealth = 35` 是卡 SO 的静态数据，不是运行时中间态。**
- **同族一致性（真值 60 个督军）**：`maxHealth` 分布 `20×6 / 30×3 / 35×40 / 40×11`；**`armour` 60 个全 0**。
  35 是**主流值**（40/60）。其余 3 个 AM 督军（`AM1` 40 · `AM2` 35 · `AM66` 35）**我方与真值一致**，
  **只有 AM3 我方 30 ≠ 真值 35**。PnP 派生表那三张也与真值一致（40/35/35）⇒ **差的是 AM3 这一张、不是我们整表偏**。
- **为什么倾向「30 是对的」**：本仓**已有同型先例 `A1876`**（`资料/普查产出_第二十九会话/A1876_取值不同_逐条.md:3`）：
  「22 条核完 —— **我们对，卡图对我们，一条都不用改**……是**两个来源的版本差**：对账用的『原版』列是旧快照，
  我们跟的是 **PnP 现行印刷版**」。AM3 的形状**一模一样**（PnP 站我们、快照孤例）。
  并且我**独立测出了「PnP 比快照新」**：PnP 里有 **Space Wolves 督军**（`Logan Grimnar`/`Njal Stormcaller`/`Ragnar Blackmane`），
  而快照**整族 Space Wolves 都没有**（本仓已记）⇒ **PnP = 更晚的版本**。其余四张对得上的卡（Baneblade 6/12/2/10、Leontus 40、Ursula 35、Hektor 35）
  说明 PnP 不是乱印的。⇒ **35 → 30 是一次再平衡（削弱），现行印面 30，我方 30 应保留。**
  ⚠️ **反面可能无法完全排除**：PnP 这一张单独印错（把 35 印成 30）。**本地没有能直接反驳它的一条**——
  现行客户端的 `allcards` 包**不在本地**（本仓已记：`assets_full` 与真游戏安装都没有）。

### ⇒ 结论倾向

- **护甲**：两侧都是 0，**我们的 `armor 2` 是 OCR 误读、要清掉**。强度：**很强**。
- **健康**：倾向**保留 30**（跟 PnP 现行印刷，同 `A1876` 先例；且 PnP 已被证明比快照新）。强度：**中**（≈65–70%）。
  若主对话更看重「唯一一份原版机器可读数据」⇒ 也可取 35，但那就与本仓 `A1876` 的既有口径**不一致**。

## 没查清

- **搜过哪些词**：`Elan` · `Commissar Elan` · `Commissar Denkler` · `Denkler` · `Deckler` · `maxHealth`（大小写两种）· `armour`/`Armour` · `WarlordLife`。
- **搜过哪些位置**：`d:/2/新解包资源/assets_full`（**24.0 万个 `.json` 正文全扫** + 全盘**文件名**扫）· `d:/4/Unity/**`（全后缀正文扫）·
  `d:/2/tools/decomp_full`（2.5 万个 `.c` 全扫）· `d:/2/Warpforge_tools` · `d:/2/Warpforge_code` · `d:/warpforge` · `d:/2/解包整理` · **开图** `d:/2/Warpforge部队卡片`。
- **仍没查清的 4 条**：
  1. **现行客户端（Unity 6）里 AM3 到底 30 还是 35** —— 需要 `allcards` 包，**本地没有**（真游戏安装 84 个包也不含）。
  2. `BattleManager__SetupWarlordsLifeAndEnchantment` 里 `lVar2 + 0x1c ∈ {0x50, 0x32}` 与 `+0x20` 的**字段名**读不出（反编译无符号）。
  3. PnP 的**出版/修订日期**只能从 mtime 看到 `2026-08-15`（目录里另有 `UPDATED_…Guilliman.png` 说明它会改版），**版本号只知 `1.5-3`**。
  4. AM3 **不在教程 SO** 里（只有 `UM46`/`UM47`）⇒ 那一族对它**没有证据**。

## 顺手发现（不在本次范围，交调度台分流）

1. 🔴 **`AM66` 反证了「拿真值 `cardName` 当卡名判据」这条口径**：真值 `AM66.cardName = "Tempestor Prime"`，
   但官方 **`Card_Name/AM66 = "Hektor Thenmann"`**、PnP 也印 `Hektor Thenmann`。
   ⇒ **卡 SO 的 `cardName` 不总是显示名**（玩家看到的是 `Card_Name/*`）⇒
   `资料/普查产出_第三十一会话/R_卡牌线判据审计.md:92` 那套「拿真值 `cardName` 逐条比我们的名」会把 AM66 这类**误判成不一致**。
2. 🔴 **`cards_1195_2022时代.json` 里的「2022」是 Unity 版本（`2022.3.38f1`）、不是年代**；
   缓存时间 **2025-07-30**（判据 = `d:/2/远端缓存_旧profile_1013/README.md` §四）。
   `A1876_取值不同_逐条.md:3` 与 `历史/A表已收口_第三十会话.md:75` 都写成「**2022 时代快照**」⇒ 措辞应订正（结论不变）。
3. `card_stats.json` 的 `ocrSrc` 列对 AM3 指向 `…/1督军/Warpforge_01_Commissar-Denkler.png`（**有效的**），
   与 `A1876_取值不同_逐条.md` §五·A 记的「`ocrSrc` 已失效 1119/1121」**不一致**（本张没失效）—— 可能是旧目录名 `Core/` 的残留口径（见 `community_cards.json:18` 写的是 `Astra Militarum\Core\…`）。
4. `AM17 Commissar` 是**部队**（不是督军），`Avatar_AM_Commissar` / `Avatar_AM_Upsale_Commissar` 属它，**别与 AM3 混**。

**给主对话的裁决建议**：①**卡名改成官方 `Commissar Denkler`**（`nameZh` 不动）—— 这条我**很确定（≈95%）**；
②**护甲按 0 处理、把 `card_stats.json` 的 `armor 2` 当 OCR 误读清掉** —— **很确定（≈95%）**；
③**生命保留 30**（跟 PnP 现行版，同 `A1876` 先例）—— 我只**中等确定（≈65–70%）**：反对面（快照 35）也没被排除，
**这一条最终请主对话裁**，并把「若将来拿到现行 `allcards` 要复核 AM3 生命」记成挂起尾巴。
