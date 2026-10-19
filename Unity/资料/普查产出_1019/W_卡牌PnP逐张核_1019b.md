# W · 卡牌 SO ↔ PnP 逐张对账（1019b）

> 只读普查。判据 = `d:/2/Warpforge部队卡片/` 的 900×1200 成品卡图（逐张亲读）。
> 卡图读法（本次逐张确认）：**蓝/青六边形 = 费用**（右缘上 1/3 处）；底排 **左红圆剑 = 近战 · 紫圆枪 = 远程 · 右下银盾 = 护甲 · 最右绿框 = 生命**；底中菱形 = 稀有度宝石；**右上角绿色/阵营纹章是固定图案（所有卡都像 “5”，不是费用）**。
> ⚠️ **督军卡不印费用**（只印近战/远程/生命）；**Dark Pact 系列不印任何费用**。

---

## §一 汇总

### 1. 覆盖
- **82 张清单**：14 张此前已核 + **68 张本次逐张开图核完 = 82/82 全覆盖**。
  （14 张里本次**重开图复核了 3 张**：`Kommando`、`Strike Team`、`UM32 Tactical Insight`；其余 11 张沿用原报告结论，未重开图。）
- **当前 build 的 60 项不一致**：全部并入本表（其中 15 张与 82 清单重叠，另 45 张本次新核）⇒ **去重后共 127 张唯一卡**。
- **缺图 = 0**。两张无正式 PnP 文件的卡在阵营目录的截图里找到并读完：`Lhykhis`（`Aeldari/1督军/Zrzut ekranu …18.43.31.png`）、`Orian Laratharjos`（`Aeldari/3部队/Zrzut ekranu …18.55.24.png`）。

### 2. 结论数字

| 口径 | 数量 |
|---|---:|
| 本次 68 张：**PnP=我们**（=现值=当前 SO，旧 SO 错） | **54** |
| 本次 68 张：**PnP=旧我们 ≠ 当前 SO/当前我们** ⚠️ | **10** |
| 本次 68 张：**PnP 读不出**（Dark Pact 无费用） | **4** |
| 本次 68 张：找不到图 | **0** |
| **82 张合计：PnP=我们**（含 14 张复核） | **67**（68 张里 54 + 14 张里 13） |
| **82 张合计：PnP=旧我们 ≠ 当前值** | **11**（10 新核 + `UM32 Tactical Insight`） |
| **82 张合计：PnP 读不出** | **4** |
| **82 张合计：PnP=旧 SO（=我们抄错）** | **0** |
| **60 项**：与 PnP 冲突 | **56** |
| **60 项**：PnP 读不出 | **4**（Dark Pact ×4） |
| **60 项**：与 PnP 一致 | **0** |

> **去重全表 127 张唯一卡（82 清单 ∪ 60 项，交集 15）：**
> **PnP=当前值 67 · PnP=覆盖前旧值≠当前值 56 · PnP 读不出 4 · 找不到图 0。**
> **`PnP=SO（我们抄错）` = 0 张。**

### 3. 🔴 最重要的一条
**2026-10-18 的「SO 覆盖」把 56 处值改成了与 PnP 印刷卡冲突的值。**
- 覆盖前（`da25199`）我们 = PnP；覆盖后（`4995622` 起）= 当前 build SO。
- **当前 `cards_engine.json` 与当前 build SO 在五项数值上 1127/1127 完全相同**（本次现算：0 处不一致）⇒ 「我们 vs 当前 SO」已不再有区分度，**唯一能裁的就是 PnP**。
- PnP 在所有可读的冲突处**一律支持覆盖前的旧值**；口径分两层写清：
  · **82 张清单**（旧 SO 与旧我们冲突的那批）：**0 处支持旧 SO** —— 旧快照是全错的；
  · **60 项**（当前 build SO 与覆盖前我们冲突的那批）：**0 处支持当前 build SO**。
  （另 45 张督军不在 82 清单里，它们的**旧 SO 恰好 = 覆盖前我们 = PnP**，是 2026 build 才被改下去的。）
- 两种解释，需用户拍板：
  ① **按铁律 7「PnP 成品图是最终真相」** ⇒ 这 56 处覆盖是**新引入的真缺陷**，应回滚；
  ② 若认定 **2026 build 的 SO 是更新的平衡补丁、PnP（1.5-3，截图 2026-04-16）已过期** ⇒ 保留现值，但要在文档里把「PnP 不再是数值终审」写死（目前 `R_督军10血成因.md` 的裁定没经过 PnP 对账）。

### 4. 真缺陷清单（56 项 / 56 张，逐项 = 当前值 → PnP 值）

**Health（49）**
`AM3 Commissar Denkler` 25→30 · `BL71 Havoc Champion` 8→7 · `GOF76 Mozrog Skragbad` 30→40 · `TL35 Swarmlord` 25→40 · `SW13 Wolf Guard Headtaker` 3→4
`ASH1 Anvirr Keltoc` 25→35 · `ASH2 Eliac Zephyrblade` 30→40 · `ASH3 Medreyal Ghaelyn` 25→35 · `ASH74 Jain Zar` 25→35
`AM2 Ursula Creed` 25→35 · `AM66 Hektor Thenmann` 25→35 · `AM1 Lord Solar Leontus` 30→40
`BL34 Abaddon the Despoiler` 30→40 · `BL35 Haarken Worldclaimer` 25→35 · `BL36 Sylar Hexscorn` 25→35 · `BL65 Ghallaron the Pious` 25→35
`DA3 Asmodai` 25→35 · `DA1 Azrael` 30→40 · `DA2 Belial` 25→35
`GSC3 Acolyte Iconward` 25→35 · `GSC66 Lhaska Szenari` 25→35 · `GSC2 Magus Uthrel Naas` 25→35 · `GSC1 Primus Saffa Rhiannor` 25→35
`SAU35 Imotekh the Stormlord` 30→40 · `SAU36 Nemesor Zahndrekh` 25→35 · `SAU37 Orikan the Diviner` 25→35 · `SAU66 Ramatekh The Cruel` 25→35
`GOF28 Ghazghkull Thraka` 30→40 · `GOF77 Beastboss Morgrim` 25→35 · `GOF27 Boss Zagstruk` 25→35 · `GOF29 Grukk Face-Rippa` 25→35 · `GOF75 Warboss Gordrang` 25→35
`SOR65 Celestian Sacresant Aveline` 25→35 · `SOR1 Erika Luminas` 25→35 · `SOR2 Junith Eruita` 25→35 · `SOR3 Morvenn Vahl` 30→40
`TAU2 Aun'Va` 25→35 · `TAU3 Commander O'Maisos` 25→35 · `TAU1 Shadowsun` 30→40 · `TAU65 War Shaper` 20→30
`TL74 Terror of Vardenghast` 25→35 · `TL36 Neurothrope` 25→35 · `TL34 Tervigon` 30→40
`UM76 Lieutenant Titus` 25→35 · `UM65 Chaplain Letharius` 25→35 · `UM46 Marneus Calgar` 30→40 · `UM87 Roboute Guilliman` 30→40 · `UM47 Uriel Ventris` 25→35 · `UM48 Varro Tigurius` 25→35

**Cost（6）**
`Infernal Gaze` 1→2 · `Spreading Corruption` 3→4 · `Tidewall Gunrig` 1→0 · `Neurogaunt` 1→2 · `Synaptic Imperative` 1→2 · `Tactical Insight` 1→0

**Melee（1）**
`TAU12 Darkstrider` 近战 2→3

---

## §二 逐卡表

> 数值列顺序 = **费用 / 近战 / 远程 / 护甲 / 生命**。「—」= 卡面未印（督军无费用；Dark Pact 无费用）。
> 「我们」= 现读 `cards_engine.json`；「当前SO」= `cards_raw.json`（两者五项**全部相同**，见 §四·1）。
> 「旧SO」只写与本表 PnP 不同的项（来自 `ID_我们的数据链与原版SO.md:85-112`）。

### 2·A 本次新核的 68 张

| 卡名 | 阵营 | 费用 | 近战 | 远程 | 护甲 | 生命 | 我们 | 旧SO | 当前SO | 裁决 | 图路径 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Howling Banshee | Aeldari | 4 | 3 | 2 | 0 | 4 | 4/3/2/0/4 | melee:4 | 4/3/2/0/4 | PnP=我们 ✅ | Warpforge部队卡片/Aeldari/3部队/Warpforge_20_Howling-Banshee.png |
| Orian Laratharjos | Aeldari | 4 | 2 | 4 | 0 | 5 | 4/2/4/0/5 | ranged:5 | 4/2/4/0/5 | PnP=我们 ✅ | …/Aeldari/3部队/Zrzut ekranu 2026-04-16 o 18.55.24.png（截图，卡面可读） |
| Wraithguard | Aeldari | 4 | 3 | 3 | 0 | 4 | 4/3/3/0/4 | melee:2 | 4/3/3/0/4 | PnP=我们 ✅ | …/Aeldari/3部队/Warpforge_25_Wraithguard.png |
| Wave Serpent | Aeldari | 7 | 3 | 6 | 0 | 8 | 7/3/6/0/8 | health:7 | 7/3/6/0/8 | PnP=我们 ✅ | …/Aeldari/3部队/Warpforge_37_Wave-Serpent.png |
| Lhykhis | Aeldari | — | 2 | 2 | 0 | 25 | 2/2/2/0/25 | health:35 | 2/2/2/0/25 | PnP=我们 ✅（费用未印） | …/Aeldari/1督军/Zrzut ekranu 2026-04-16 o 18.43.31.png（截图） |
| Commissar Denkler | Astra Militarum | — | 2 | 2 | 0 | 30 | 2/2/2/0/25 | health:35 | 2/2/2/0/25 | **PnP=旧我们≠当前** ⚠️ | …/Astra Militarum/1督军/Warpforge_01_Commissar-Denkler.png |
| Havoc Champion | Chaos | 7 | 8 | 6 | 0 | 7 | 7/8/6/0/8 | health:8 | 7/8/6/0/8 | **PnP=旧我们≠当前** ⚠️ | …/Chaos/3部队/Warpforge_9_Havoc-Champion.png |
| Infernal Gaze | Chaos | 2 | 0 | 0 | 0 | 0 | 1/0/0/0/0 | cost:1 | 1/0/0/0/0 | **PnP=旧我们≠当前** ⚠️ | …/Chaos/2天赋/Warpforge_7_Infernal-Gaze.png |
| Spreading Corruption | Chaos | 4 | 0 | 0 | 0 | 0 | 3/0/0/0/0 | cost:3 | 3/0/0/0/0 | **PnP=旧我们≠当前** ⚠️ | …/Chaos/4计策/Warpforge_13_Spreading-Corruption.png |
| Duelist's Hubris | Chaos | 1 | 0 | 0 | 0 | 0 | 1/0/0/0/0 | cost:2 | 1/0/0/0/0 | PnP=我们 ✅ | …/Emperor_s Children/2天赋/Warpforge_04_Duelists-Hubris.png |
| Dark Pact of Fate | Chaos | — | 0 | 0 | 0 | 0 | -1/0/0/0/0 | cost:-1 | -1/0/0/0/0 | PnP 读不出（卡面无费用） | …/Chaos/4计策/Warpforge_16_Dark-Pact-of-Fate.png |
| Dark Pact of Blood | Chaos | — | 0 | 0 | 0 | 0 | -1/0/0/0/0 | cost:-1 | -1/0/0/0/0 | PnP 读不出（卡面无费用） | …/Chaos/4计策/Warpforge_18_Dark-Pact-of-Blood.png |
| Dark Pact of Excess | Chaos | — | 0 | 0 | 0 | 0 | -1/0/0/0/0 | cost:-1 | -1/0/0/0/0 | PnP 读不出（卡面无费用） | …/Chaos/4计策/Warpforge_20_Dark-Pact-of-Excess.png |
| Dark Pact of Resilience | Chaos | — | 0 | 0 | 0 | 0 | -1/0/0/0/0 | cost:-1 | -1/0/0/0/0 | PnP 读不出（卡面无费用） | …/Chaos/4计策/Warpforge_22_Dark-Pact-of-Resilience.png |
| Lucius The Eternal | Emperors Children | — | 3 | 1 | 0 | 25 | 2/3/1/0/25 | health:35 | 2/3/1/0/25 | PnP=我们 ✅ | …/Emperor_s Children/1督军/Warpforge_03_Lucius-The-Eternal.png |
| Xarahan, Daemon Prince | Emperors Children | — | 2 | 2 | 0 | 30 | 2/2/2/0/30 | health:40 | 2/2/2/0/30 | PnP=我们 ✅ | …/Emperor_s Children/1督军/Warpforge_05_Xarahan-Daemon-Prince.png |
| Lord Kaphrael | Emperors Children | — | 2 | 2 | 0 | 25 | 2/2/2/0/25 | health:35 | 2/2/2/0/25 | PnP=我们 ✅ | …/Emperor_s Children/1督军/Warpforge_01_Lord-Kaphrael.png |
| Interrogator Chaplain | Dark Angels | 4 | 4 | 3 | 0 | 5 | 4/4/3/0/5 | ranged:2 | 4/4/3/0/5 | PnP=我们 ✅ | …/Dark Angels/3部队/Warpforge_22_Interrogator-Chaplain.png |
| Covert Operation | Dark Angels | 3 | 0 | 0 | 0 | 0 | 3/0/0/0/0 | cost:4 | 3/0/0/0/0 | PnP=我们 ✅ | …/Dark Angels/4计策/Warpforge_56_Covert-Operation.png |
| Master Interromancer | Dark Angels | 1 | 0 | 0 | 0 | 0 | 1/0/0/0/0 | cost:3 | 1/0/0/0/0 | PnP=我们 ✅ | …/Dark Angels/2天赋/Warpforge_35_Master-Interromancer.png |
| Wages of Retribution | Dark Angels | 3 | 0 | 0 | 0 | 0 | 3/0/0/0/0 | cost:4 | 3/0/0/0/0 | PnP=我们 ✅ | …/Dark Angels/4计策/Warpforge_60_Wages-of-Retribution.png |
| Aberrant | Genestealer Cult | 5 | 6 | 0 | 1 | 6 | 5/6/0/1/6 | health:5 | 5/6/0/1/6 | PnP=我们 ✅ | …/Genestealer Cult/3部队/Warpforge_27_Aberrant.png |
| Acolyte Specialist | Genestealer Cult | 4 | 2 | 5 | 0 | 5 | 4/2/5/0/5 | melee:1 | 4/2/5/0/5 | PnP=我们 ✅ | …/Genestealer Cult/3部队/Warpforge_23_Acolyte-Specialist.png |
| Hybrid Metamorph | Genestealer Cult | 4 | 4 | 4 | 0 | 4 | 4/4/4/0/4 | melee:3 | 4/4/4/0/4 | PnP=我们 ✅ | …/Genestealer Cult/3部队/Warpforge_24_Hybrid-Metamorph.png |
| Neophyte Heavy | Genestealer Cult | 3 | 1 | 5 | 0 | 4 | 3/1/5/0/4 | health:3 | 3/1/5/0/4 | PnP=我们 ✅ | …/Genestealer Cult/3部队/Warpforge_18_Neophyte-Heavy.png |
| Subterranean Ambush | Genestealer Cult | 4 | 0 | 0 | 0 | 0 | 4/0/0/0/0 | cost:5 | 4/0/0/0/0 | PnP=我们 ✅ | …/Genestealer Cult/4计策/Warpforge_62_Subterranean-Ambush.png |
| Damaged Hexmark | Necron | 5 | 1 | 5 | 0 | 4 | 5/1/5/0/4 | health:5 | 5/1/5/0/4 | PnP=我们 ✅ | …/Necron/3部队/Warpforge_06_Damaged-Hexmark.png |
| Immortal Pride | Necron | 7 | 0 | 0 | 0 | 0 | 7/0/0/0/0 | cost:6 | 7/0/0/0/0 | PnP=我们 ✅ | …/Necron/4计策/Warpforge_57_Immortal-Pride.png |
| Grot on Bombsquig | Orks | 3 | 1 | 2 | 0 | 2 | 3/1/2/0/2 | melee:2 | 3/1/2/0/2 | PnP=我们 ✅ | …/Orks/3部队/Warpforge_08_Grot-on-Bombsquig.png |
| Nob on Smasha Squig | Orks | 5 | 6 | 3 | 1 | 5 | 5/6/3/1/5 | melee:5 | 5/6/3/1/5 | PnP=我们 ✅ | …/Orks/3部队/Warpforge_13_Nob-on-Smasha-Squig.png |
| Snakebite Battlewagon | Orks | 9 | 6 | 9 | 0 | 11 | 9/6/9/0/11 | health:10 | 9/6/9/0/11 | PnP=我们 ✅ | …/Orks/3部队/Warpforge_21_Snakebite-Battlewagon.png |
| Mozrog Skragbad | Orks | — | 2 | 2 | 0 | 40 | 2/2/2/0/30 | health:35 | 2/2/2/0/30 | **PnP=旧我们≠当前** ⚠️ | …/Orks/1督军/Warpforge_02_Mozrog-Skragbad.png |
| Beast Snagga Nob | Orks | 3 | 4 | 2 | 0 | 4 | 3/4/2/0/4 | ranged:1 | 3/4/2/0/4 | PnP=我们 ✅ | …/Orks/3部队/Warpforge_07_Beast-Snagga-Nob.png |
| Snakebite Nob | Orks | 4 | 4 | 3 | 0 | 5 | 4/4/3/0/5 | health:4 | 4/4/3/0/5 | PnP=我们 ✅ | …/Orks/3部队/Warpforge_10_Snakebite-Nob.png |
| Big Choppa Nob | Orks | 6 | 7 | 5 | 0 | 7 | 6/7/5/0/7 | ranged:4 | 6/7/5/0/7 | PnP=我们 ✅ | …/Orks/3部队/Warpforge_05_Big-Choppa-Nob.png |
| Painboy Sniklaw | Orks | 5 | 6 | 3 | 0 | 6 | 5/6/3/0/6 | melee:5,ranged:2 | 5/6/3/0/6 | PnP=我们 ✅ | …/Orks/3部队/Warpforge_26_Painboy-Sniklaw.png |
| Rokkit Boy | Orks | 4 | 3 | 5 | 0 | 5 | 4/3/5/0/5 | melee:2,ranged:4 | 4/3/5/0/5 | PnP=我们 ✅ | …/Orks/3部队/Warpforge_03_Rokkit-Boy.png |
| Tankbusta | Orks | 6 | 5 | 5 | 0 | 6 | 6/5/5/0/6 | melee:4,ranged:4 | 6/5/5/0/6 | PnP=我们 ✅ | …/Orks/3部队/Warpforge_30_Tankbusta.png |
| Veteran Stormboy | Orks | 6 | 7 | 4 | 0 | 6 | 6/7/4/0/6 | melee:6 | 6/7/4/0/6 | PnP=我们 ✅ | …/Orks/3部队/Warpforge_31_Veteran-Stormboy.png |
| Da Green Horde | Orks | 3 | 0 | 0 | 0 | 0 | 3/0/0/0/0 | cost:4 | 3/0/0/0/0 | PnP=我们 ✅ | …/Orks/4计策/Warpforge_51_Da-Green-Horde.png |
| Gorkanaut | Orks | 9 | 10 | 5 | 1 | 10 | 9/10/5/1/10 | health:9 | 9/10/5/1/10 | PnP=我们 ✅ | …/Orks/3部队/Warpforge_38_Gorkanaut.png |
| Gunwagon | Orks | 8 | 5 | 8 | 0 | 9 | 8/5/8/0/9 | ranged:7 | 8/5/8/0/9 | PnP=我们 ✅ | …/Orks/3部队/Warpforge_36_Gunwagon.png |
| Krumpaklaw | Orks | 8 | 8 | 6 | 0 | 8 | 8/8/6/0/8 | ranged:4 | 8/8/6/0/8 | PnP=我们 ✅ | …/Orks/3部队/Warpforge_07A_Krumpaklaw.png |
| Battle Sister Gunner | Sororitas | 2 | 1 | 3 | 0 | 3 | 2/1/3/0/3 | ranged:2 | 2/1/3/0/3 | PnP=我们 ✅ | …/Sorotitas/3部队/Warpforge_10_Battle-Sister-Gunner.png |
| Celestian | Sororitas | 3 | 2 | 3 | 0 | 4 | 3/2/3/0/4 | melee:1 | 3/2/3/0/4 | PnP=我们 ✅ | …/Sorotitas/3部队/Warpforge_15_Celestian.png |
| Dominion Superior | Sororitas | 5 | 4 | 6 | 0 | 5 | 5/4/6/0/5 | ranged:5 | 5/4/6/0/5 | PnP=我们 ✅ | …/Sorotitas/3部队/Warpforge_28_Dominion-Superior.png |
| Dominion | Sororitas | 3 | 2 | 4 | 0 | 4 | 3/2/4/0/4 | ranged:3 | 3/2/4/0/4 | PnP=我们 ✅ | …/Sorotitas/3部队/Warpforge_17_Dominion.png |
| Hospitaller | Sororitas | 4 | 4 | 3 | 0 | 5 | 4/4/3/0/5 | ranged:2 | 4/4/3/0/5 | PnP=我们 ✅ | …/Sorotitas/3部队/Warpforge_21_Hospitaller.png |
| Missionary | Sororitas | 4 | 5 | 2 | 0 | 5 | 4/5/2/0/5 | health:4 | 4/5/2/0/5 | PnP=我们 ✅ | …/Sorotitas/3部队/Warpforge_22_Missionary.png |
| Sister Superior | Sororitas | 4 | 4 | 4 | 0 | 5 | 4/4/4/0/5 | health:4 | 4/4/4/0/5 | PnP=我们 ✅ | …/Sorotitas/3部队/Warpforge_25_Sister-Superior.png |
| Hallowed Martyrs | Sororitas | 5 | 0 | 0 | 0 | 0 | 5/0/0/0/0 | cost:4 | 5/0/0/0/0 | PnP=我们 ✅ | …/Sorotitas/4计策/Warpforge_56_Hallowed-Martyrs.png |
| Sacred Altar | Sororitas | 1 | 0 | 0 | 0 | 0 | 1/0/0/0/0 | cost:0 | 1/0/0/0/0 | PnP=我们 ✅ | …/Sorotitas/5防御卡/Warpforge_65_Sacred-Altar.png |
| Broadside Battlesuit | Tau | 5 | 4 | 5 | 1 | 5 | 5/4/5/1/5 | melee:3 | 5/4/5/1/5 | PnP=我们 ✅ | …/Tau/3部队/Warpforge_30_Broadside-Battlesuit.png |
| Coldstar Battlesuit | Tau | 7 | 6 | 8 | 1 | 7 | 7/6/8/1/7 | melee:5 | 7/6/8/1/7 | PnP=我们 ✅ | …/Tau/3部队/Warpforge_38_Coldstar-Battlesuit.png |
| Stealth Battlesuit | Tau | 4 | 3 | 4 | 0 | 5 | 4/3/4/0/5 | health:4 | 4/3/4/0/5 | PnP=我们 ✅ | …/Tau/3部队/Warpforge_26_Stealth-Battlesuit.png |
| Gun Drone | Tau | 2 | 0 | 2 | 0 | 3 | 2/0/2/0/3 | ranged:1 | 2/0/2/0/3 | PnP=我们 ✅ | …/Tau/3部队/Warpforge_10_Gun-Drone.png |
| Marker Drone | Tau | 2 | 0 | 2 | 0 | 3 | 2/0/2/0/3 | ranged:1 | 2/0/2/0/3 | PnP=我们 ✅ | …/Tau/3部队/Warpforge_11_Marker-Drone.png |
| Stealth Drone | Tau | 2 | 0 | 1 | 0 | 1 | 2/0/1/0/1 | health:2 | 2/0/1/0/1 | PnP=我们 ✅ | …/Tau/3部队/Warpforge_14_Stealth-Drone.png |
| Darkstrider | Tau | 3 | 3 | 4 | 0 | 3 | 3/2/4/0/3 | melee:2 | 3/2/4/0/3 | **PnP=旧我们≠当前** ⚠️ | …/Tau/3部队/Warpforge_16_Darkstrider.png |
| Fire Warrior Marksman | Tau | 4 | 2 | 5 | 0 | 5 | 4/2/5/0/5 | health:4 | 4/2/5/0/5 | PnP=我们 ✅ | …/Tau/3部队/Warpforge_23_Fire-Warrior-Marksman.png |
| Firesight Marksman | Tau | 5 | 2 | 5 | 0 | 5 | 5/2/5/0/5 | ranged:4 | 5/2/5/0/5 | PnP=我们 ✅ | …/Tau/3部队/Warpforge_32_Firesight-Marksman.png |
| Experimental Drones | Tau | 4 | 0 | 0 | 0 | 0 | 4/0/0/0/0 | cost:5 | 4/0/0/0/0 | PnP=我们 ✅ | …/Tau/4计策/Warpforge_64_Experimental-Drones.png |
| Tidewall Gunrig | Tau | 0 | 0 | 0 | 0 | 0 | 1/0/0/0/0 | cost:1 | 1/0/0/0/0 | **PnP=旧我们≠当前** ⚠️ | …/Tau/5防御卡/Warpforge_68_Tidewall-Gunrig.png |
| Sky Ray Gunship | Tau | 7 | 4 | 9 | 0 | 8 | 7/4/9/0/8 | melee:3,ranged:8 | 7/4/9/0/8 | PnP=我们 ✅ | …/Tau/3部队/Warpforge_40_Sky-Ray-Gunship.png |
| Neurogaunt | Tyranid | 2 | 1 | 0 | 0 | 2 | 1/1/0/0/2 | cost:1 | 1/1/0/0/2 | **PnP=旧我们≠当前** ⚠️ | …/Tyranid/3部队/Warpforge_4_Neurogaunt.png |
| Synaptic Imperative | Tyranid | 2 | 0 | 0 | 0 | 0 | 1/0/0/0/0 | cost:1 | 1/0/0/0/0 | **PnP=旧我们≠当前** ⚠️ | …/Tyranid/2天赋/Warpforge_38_Synaptic-Imperative.png |
| Swarmlord | Tyranid | — | 3 | 1 | 0 | 40 | 2/3/1/0/25 | health:35 | 2/3/1/0/25 | **PnP=旧我们≠当前** ⚠️ | …/Tyranid/1督军/Warpforge_3_Swarmlord.png |
| Master of the Fleet | Ultramarines | 2 | 0 | 0 | 0 | 0 | 2/0/0/0/0 | cost:1 | 2/0/0/0/0 | PnP=我们 ✅ | …/Ultramarines/2天赋/Warpforge_4_Master-of-the-Fleet.png |

<!-- 复核（14 张已核的抽样 2 张）：
Kommando = 7/8/4/0/7 = 我们 ✅（…/Orks/3部队/Warpforge_34_Kommando.png）
Strike Team = 4/2/6/0/5 = 我们 ✅（…/Tau/3部队/Warpforge_28_Strike-Team.png） -->

### 2·B 「60 项」里另外 45 张（全部因 health 覆盖而冲突）

> 全部是督军（费用不印），PnP 近战/远程一律 **2/2**、护甲 0；`Wolf Guard Headtaker` 例外（步兵）。
> 「旧SO」= 与覆盖前的我们相同（这 45 张不在 82 冲突清单里）。

| 卡名 | 阵营 | 费用 | 近战 | 远程 | 护甲 | 生命(PnP) | 我们/当前SO | 旧SO | 裁决 | 图路径 |
|---|---|---|---|---|---|---|---|---|---|---|
| Anvirr Keltoc | Aeldari | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Aeldari/1督军/Warpforge_1_Anvirr-Keltoc.png |
| Eliac Zephyrblade | Aeldari | — | 2 | 2 | 0 | 40 | 30 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Aeldari/1督军/Warpforge_3_Eliac-Zephyrblade.png |
| Medreyal Ghaelyn | Aeldari | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Aeldari/1督军/Warpforge_5_Medreyal-Ghaelyn.png |
| Jain Zar | Aeldari | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Aeldari/1督军/Warpforge_01_Jain-Zar.png |
| Ursula Creed | Astra Militarum | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Astra Militarum/1督军/Warpforge_05_Ursula-Creed.png |
| Hektor Thenmann | Astra Militarum | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Astra Militarum/1督军/Warpforge_01_Hektor-Thenmann.png |
| Lord Solar Leontus | Astra Militarum | — | 2 | 2 | 0 | 40 | 30 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Astra Militarum/1督军/Warpforge_03_Lord-Solar-Leontus.png |
| Abaddon the Despoiler | Chaos | — | 2 | 2 | 0 | 40 | 30 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Chaos/1督军/Warpforge_1_Abaddon-the-Despoiler.png |
| Haarken Worldclaimer | Chaos | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Chaos/1督军/Warpforge_3_Haarken-Worldclaimer.png |
| Sylar Hexscorn | Chaos | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Chaos/1督军/Warpforge_5_Sylar-Hexscorn.png |
| Ghallaron the Pious | Chaos | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Chaos/1督军/Warpforge_1_Ghallaron-the-Pious.png |
| Asmodai | Dark Angels | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Dark Angels/1督军/Warpforge_01_Asmodai.png |
| Azrael | Dark Angels | — | 2 | 2 | 0 | 40 | 30 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Dark Angels/1督军/Warpforge_03_Azrael.png |
| Belial | Dark Angels | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Dark Angels/1督军/Warpforge_05_Belial.png |
| Acolyte Iconward（卡面名 Iconward Malak Vorenth） | Genestealer Cult | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Genestealer Cult/1督军/Warpforge_01_Iconward-Malak-Vorenth.png |
| Lhaska Szenari | Genestealer Cult | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Genestealer Cult/1督军/Warpforge_01_Lhaska-Szenari.png |
| Magus Uthrel Naas | Genestealer Cult | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Genestealer Cult/1督军/Warpforge_03_Magus-Uthrel-Naas.png |
| Primus Saffa Rhiannor | Genestealer Cult | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Genestealer Cult/1督军/Warpforge_05_Primus-Saffa-Rhiannor.png |
| Imotekh the Stormlord | Necron | — | 2 | 2 | 0 | 40 | 30 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Necron/1督军/Warpforge_1_Imotekh-the-Stormlord.png |
| Nemesor Zahndrekh | Necron | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Necron/1督军/Warpforge_3_Nemesor-Zahndrekh.png |
| Orikan the Diviner | Necron | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Necron/1督军/Warpforge_5_Orikan-the-Diviner.png |
| Ramatekh the Cruel | Necron | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Necron/1督军/Warpforge_01_Ramatekh-the-Cruel.png |
| Ghazghkull Thraka | Orks | — | 2 | 2 | 0 | 40 | 30 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Orks/1督军/Warpforge_03_Ghazghkull-Thraka.png |
| Beastboss Morgrim | Orks | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Orks/1督军/Warpforge_01_Beastboss-Morgrim.png |
| Boss Zagstruk | Orks | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Orks/1督军/Warpforge_02_Boss-Zagstruk.png |
| Grukk Face-Rippa | Orks | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Orks/1督军/Warpforge_01_Grukk-Face-Rippa-2.png |
| Warboss Gordrang | Orks | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Orks/1督军/Warpforge_01_Warboss-Gordrang.png |
| Celestian Sacresant Aveline | Sororitas | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Sorotitas/1督军/Warpforge_01_Celestian-Sacresant-Aveline.png |
| Erika Luminas | Sororitas | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Sorotitas/1督军/Warpforge_01_Erika-Luminas.png |
| Junith Eruita | Sororitas | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Sorotitas/1督军/Warpforge_03_Junith-Eruita.png |
| Morvenn Vahl | Sororitas | — | 2 | 2 | 0 | 40 | 30 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Sorotitas/1督军/Warpforge_05_Morvenn-Vahl.png |
| Wolf Guard Headtaker | Space Wolves | 3 | 3 | 2 | 0 | 4 | 3 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Space Wolves/3部队/Warpforge_21_Wolf-Guard-Headtaker.png |
| Aun'Va | Tau | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Tau/1督军/Warpforge_1_AunVa.png |
| Commander O'Maisos | Tau | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Tau/1督军/Warpforge_4_Commander-OMaisos.png |
| Shadowsun | Tau | — | 2 | 2 | 0 | 40 | 30 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Tau/1督军/Warpforge_6_Shadowsun.png |
| War Shaper | Tau | — | 2 | 2 | 0 | 30 | 20 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Tau/1督军/Warpforge_01_War-Shaper.png |
| Terror of Vardenghast | Tyranid | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Tyranid/1督军/Warpforge_1_Terror-of-Vardenghast.png |
| Neurothrope | Tyranid | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Tyranid/1督军/Warpforge_1_Neurothrope.png |
| Tervigon | Tyranid | — | 2 | 2 | 0 | 40 | 30 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Tyranid/1督军/Warpforge_5_Tervigon.png |
| Lieutenant Titus | Ultramarines | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Ultramarines/1督军/Warpforge_00_Lieutenant-Titus-2.png |
| Chaplain Letharius | Ultramarines | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Ultramarines/1督军/Warpforge_01_Chaplain-Letharius-2.png |
| Marneus Calgar | Ultramarines | — | 2 | 2 | 0 | 40 | 30 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Ultramarines/1督军/Warpforge_1_Marneus-Calgar.png |
| Roboute Guilliman | Ultramarines | — | 2 | 2 | 0 | 40 | 30 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Ultramarines/1督军/UPDATED_Warpforge_01_Roboute-Guilliman.png |
| Uriel Ventris | Ultramarines | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Ultramarines/1督军/Warpforge_3_Uriel-Ventris.png |
| Varro Tigurius | Ultramarines | — | 2 | 2 | 0 | 35 | 25 | =旧值 | PnP=旧我们≠当前 ⚠️ | …/Ultramarines/1督军/Warpforge_5_Varro-Tigurius.png |

（另：`UM32 Tactical Insight` 属 82 清单、14 张已核之一，但也是 60 项之一 —— PnP 费用 **0**，现值 **1**，同样是覆盖引入的冲突，已计入 56。）

---

## §三 读不出 / 缺图

### 读不出（4 张，均是「费用」一项）
| 卡名 | 现象 | 现/旧值 | 图路径 |
|---|---|---|---|
| Dark Pact of Fate | 卡面**完全没有费用六边形**；类型印 `Dark Pact`（附魔模板） | -1 / 旧SO -1（覆盖前我们 1） | d:/2/Warpforge部队卡片/Chaos/4计策/Warpforge_16_Dark-Pact-of-Fate.png |
| Dark Pact of Blood | 同上 | 同上 | …/Warpforge_18_Dark-Pact-of-Blood.png |
| Dark Pact of Excess | 同上 | 同上 | …/Warpforge_20_Dark-Pact-of-Excess.png |
| Dark Pact of Resilience | 同上 | 同上 | …/Warpforge_22_Dark-Pact-of-Resilience.png |

⇒ 这 4 张的 `manaCost = -1` 是**哨兵值**，卡面不可裁；保留 -1 与「卡面不印费用」不矛盾，但**不能**拿它证明 PnP 支持 -1。

### 缺图
**0 张。** 所有 127 张都在 `d:/2/Warpforge部队卡片/` 内找到可读图。
- 2 张没有独立命名文件、但在阵营目录截图里：`Lhykhis`、`Orian Laratharjos`（见 §二·A）。
- 试过的路径：全树 `d:/2/Warpforge部队卡片/**/*.png`（1,375 张）按归一化卡名匹配；两处先做模糊匹配无果，再逐一打开同目录 12 张 `Zrzut ekranu*.png` 人工确认。

---

## §四 顺手发现

1. **现读 `cards_engine.json` 与当前 build SO 的五项数值 1127/1127 完全一致（0 处不一致）。**
   即 2026-10-18 的 SO 覆盖已全量落地；「我们 vs 当前 SO」从此不再能当判据，**只剩 PnP 能裁**。
   （覆盖发生在 commit `4995622`「SO 真值旁路」，不是 `962933d`；60 项 = 49 health + 10 cost + 1 melee。）

2. **PnP 印刷卡（rulebook 1.5-3，截图时间 2026-04-16）在这 56 处一律站在「覆盖前的我们」一边。**
   没有任何一处 PnP 支持旧 SO 快照（82 张里 0 处），也没有任何一处 PnP 支持当前 build SO（60 项里 0 处）。
   ⇒ 「SO 是真值」的裁定（`R_督军10血成因.md`）**没有经过 PnP 对账**；若按铁律 7，这 56 处应回滚；若按「2026 build 更新」则需明确写下「PnP 版本已过期」的新口径。**这是本次最需要用户拍板的一条。**

3. **P（督军）血量有清晰的模式，不是随机**：PnP 一律 35（少数 40 / `War Shaper` 30），当前 SO 一律减 10（35→25、40→30、30→20）；`Swarmlord` 是 −15（40→25）。PnP 没有任何一张支持「−10」后值。
   `Wolf Guard Headtaker` 是唯一非督军的 health 冲突（4→3）。

4. **旧 SO 快照的 82 处全部是它错**：可读的 78 处（82 − 4 读不出）里，PnP 全部支持我们的值。这反过来证明 `gen_cards_engine.py` 让「PnP 赢」的既有策略是对的 —— **不要因为这次 60 项覆盖而反过来质疑那 82 张。**

5. **卡名漂移（不影响数值，但影响按名对图）**：
   - `GSC3`：我们/SO 叫 `Acolyte Iconward`，PnP 卡面印的是 `Iconward Malak Vorenth`（同阵营同槽位，数值 35 吻合）。
   - `GOF20`：旧报告写 `Veteran Flyboy`，现名/PnP 都是 `Veteran Stormboy`。
   - 文件名有 `-2` / `UPDATED_` 后缀的重印版（`Grukk Face-Rippa-2`、`Roboute Guilliman`），本次读的就是重印版。

6. **卡图读法备忘（下次别再读错）**：右上角那个绿色/阵营纹章**每张卡都长得像「5」，它不是费用**；费用是右缘中上的**蓝/青六边形**。督军卡**不印费用**；`Dark Pact` 系列也**不印费用**。底中菱形是稀有度宝石，不是护甲；护甲是右下角绿框**上方**的银盾（有数值才显示）。

7. **没有发现任何一张卡的插画/卡名与卡表错配**（本次只核数值，但逐张打开时顺带核了卡名与阵营行，均可对上）。
