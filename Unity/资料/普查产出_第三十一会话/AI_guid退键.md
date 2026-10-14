# AI · `guid_map.tsv` 的「退键」影响面普查（只读）

> 第三十一会话 · 卡牌线。**未改任何文件**（除本报告）。⛔ 没跑 Unity / 没动 git / 没跑任何生成器；
> 唯一「跑」的是 `工具/_guid_resolve_probe.py` 的 `lookup()` **函数本体**（只读调用，未走 `map` 子命令、未写盘）。
> 行号一律现读。判据源 = `_guid_resolve_probe.py` · `catalog_main.json` · 本地束（UnityPy 只读加载） · PnP 成品卡图。

---

## 〇 · 一句话

`guid_map.tsv` **4,985 行里有 56 行（1.12%）是退键**，**全部集中在 `chaosspacemarinesemperorschildrencardassets_assets_all.bundle` 这一个束**，
只波及 **3 个字段**（`cardImageReference` 48 · `summonAudio` 6 · `deathAudio` 2）、**31 个 GUID / 27 张卡**。
**我们卡池（1127 张）里 23 张**的立绘走这条路 —— 逐张核过：**23/23 拿对了，0 拿错**。
（反直觉的一条：被判「退键」的那一方反而是**对的**，被判 `exact=True` 的那一方才是脏值 —— 见 §3。）

---

## 1 · 退键的准确定义（现读 `_guid_resolve_probe.py`）+ 影响面

### 1.1 判据（⛔ 不是「2 字符」）

`lookup()` 干的是：把 GUID 映射成「束 + 束内容器键 + 资产名」。

- **`:198`** `ks = sorted([k for k in cont if guid.startswith(k)], key=lambda k: (k != r['iid'], -len(k)))`
  ⇒ 候选 = **束内所有「是这条 GUID 前缀」的容器键**；排序**先目录给的 internalId、再按长度从长到短**。
- **`:203-220`** 逐个键收「类型对得上」的对象；**`:221`** `if got and exp: break` ⇒ 拿到的**第一个键就停**。
- **`:219`** `exact=(k == r['iid'])` ⇒ **`exact=False` ⟺ 用的键不是目录给的 internalId**，也就是**退过键**。

⇒ 🔴 **准确判据 = `containerKey ≠ 目录里这条 GUID 的 internalId`**。
（`guid_map.tsv` **没有 `exact` 列**，所以要判退没退键，只能**回目录比 internalId**。）

**为什么会有退键**：容器键本身就是**短 GUID 前缀**（实测该束 187 个键全是 1–3 字符十六进制：`'0'`/`'00'`/`'0ba'`…）。
目录里的 `internalId` 与束里的容器键**对少数资产不一致** —— 例：
`db92a11e…`（EC16）目录给的 `internalId = 'd'`，而束里 `'d'` 指向一个 **AudioClip**、该资产的键其实是 **`'db9'`**
⇒ `'d'` 类型不对（`:221` 不 break）⇒ 退到最长的前缀键 `'db9'`（`exact=False`）。
（本探针 `:18` 的注释「键就是目录里的 internalId」**对大多数行成立、对这 31 个 GUID 不成立**。）

### 1.2 影响面（现读 `guid_map.tsv`）

| 项 | 数 |
|---|---|
| 总数据行 | **4,985**（+ 表头 1 行；行尾 **LF**，无 CRLF） |
| 不同 GUID | 2,372 |
| **退键行** | **56**（1.12%） |
| 退键行的字段 | `cardImageReference` **48** · `summonAudio` **6** · `deathAudio` **2** |
| 退键行的束 | **56/56 全在 `chaosspacemarinesemperorschildrencardassets_assets_all.bundle`**（其余 83 个束 0 行） |
| 退键行的类型 | Texture2D 24 · Sprite 24 · AudioClip 8 |
| 退键行的键长 | **1 字符 ×1 · 2 字符 ×29 · 3 字符 ×26** |
| 退键行的不同 GUID / 卡 | 31 / **27**（其中 23 张在我们卡池里） |

🔴 **「2 字符 = 退键」这条口径不成立**（`AA_A1770.md:177` 那句是**线索不是判据**）：

| 键长 | 退键行 | **非退键行** |
|---|---|---|
| 1 字符 | 1 | **17** |
| 2 字符 | 29 | **172** |
| 3 字符 | 26 | 36 |
| 32 字符（全 GUID） | 0 | 4,704 |

⇒ 按「≤2 字符」判：**会多报 189 行、漏报 26 行**（真阳 30 / 假阳 **189** / 假阴 26）。
🔴 **2026-10-15 订正（铁律 5，代理 `AN` 现核）**：**原写「假阳 171」，与本文件自己的逐长度表（1 字符 17 + 2 字符 172 = 189）不自洽**；用 `AN` 新加进 TSV 的 **`exact` 列**现算 = **真阳 30 / 假阳 189 / 假阴 26**（自洽检查 `(containerKey≠internalId) ⟺ exact=='False'` 不符行 = **0**）。

### 1.3 判据已用探针本体验证（不只是我复算）

直接 `import` 该文件并调 `lookup()`（只读）：

```
EC16 db92a11e1e9cf5549b301cc5d0f278fd → key=db9 exact=False → CSM_EmperorsChildren_inf_Dual Screamer Kakophonist
EC9  c651ff8dc6e037b4aaebf479f55cebd8 → key=c6  exact=False → CSM_EmperorsChildren_inf_Icon of Excess Infractor
EC56 c6bb19dd848ad4c8fb2de98c9e5c96df → key=c6  exact=True  → 同一个资产
EC35 f0bab80181747634fbb0b62bb9e2b6d1 → key=f0  exact=False → CSM_EmperorsChildren_dae_Blissbringer
EC60 f0a86ec5e49d1464eaae7c24036e5c56 → key=f0  exact=True  → 同一个资产
EC29(CK) 0d720e0b… / EC51 e175e716…  → exact=True（对照，正常）
EC55audio 2398ef13… → key=23 exact=False · EC56audio 44a4f143… → key=4 exact=False
```
⇒ 与我用「目录 internalId vs 容器键」算出的 56 行**逐条一致**。

---

## 2 · 我们池里逐卡清单（判据 = 卡池 1127 张 × 退键 GUID）

**条数 = 23 张**（`RuleEngine/Resources/cards_engine.json` 现读 `count=1127`）。

前 20 张（全量 23 张都在下表，标 PnP 核过的）：

| # | 池 id | 我们卡名 | 退键 → 解出的资产名 | PnP/对拍 |
|---|---|---|---|---|
| 1 | `EC2` | Blastmaster Noise Marine | `…_inf_Blastmaster Noise Marine` | ✔ 名逐字吻合 |
| 2 | `EC9` | Icon of Excess Infractor | `…_inf_Icon of Excess Infractor` | ✔ **开图亲核**（§3） |
| 3 | `EC11` | Flawless Blade | `…_inf_Flawless Blade` | ✔ 名吻合 |
| 4 | `EC14` | Lord Kakophonist | `…_inf_Lord Kakophonist` | ✔ **开图亲核**（§3） |
| 5 | `EC15` | Winged Daemon Prince | `…_dae_Winged Daemon Prince` | ✔ 名吻合 |
| 6 | `EC16` | Dual Screamer Kakophonist | `…_inf_Dual Screamer Kakophonist` | ✔ **开图亲核**（§3） |
| 7 | `EC18` | Glittering Myriad Infractor | `…_inf_Glittering Myriad Infractor` | ✔ 名吻合 |
| 8 | `EC21` | Malgarash the Adamant | `…_inf_Malgarash the Adamant` | ✔ 名吻合 |
| 9 | `EC24` | Slaanesh's Spawn | `…_dae_Slaanesh's Spawn` | ✔ 名吻合 |
| 10 | `EC27` | Rugged Disharmonist | `…_inf_Rugged Disharmonist` | ✔ 名吻合 |
| 11 | `EC32` | Daemonette | `…_dae_Daemonette` | ✔ 名吻合 |
| 12 | `EC33` | Alluress | `…_dae_Alluress` | ✔ 名吻合 |
| 13 | `EC34` | Fiend | `…_dae_Fiend` | ✔ 名吻合 |
| 14 | `EC35` | Blissbringer | `…_dae_Blissbringer` | ✔ **开图亲核**（§3） |
| 15 | `EC36` | Chosen of Slaanesh | `…_strat_Chosen of Slaanesh` | ✔ 名吻合 |
| 16 | `EC38` | Pledge to the Dark Prince | `…_strat_Pledge to the Dark Prince` | ✔ 名吻合 |
| 17 | `EC40` | Embrace the Pain | `…_strat_Embrace the Pain` | ✔ 名吻合 |
| 18 | `EC55` | Lucius The Eternal | `…_warlord_Lucius The Eternal` | ✔ 名吻合 |
| 19 | `EC61` | Decadent Throne | `…_strat_Dark Prince´s Throne` | ✔ 名吻合（原版旧名） |
| 20 | `aa45079b714e3ff498549e533c5954a1` | Xylocil | `…_strat_Elixir_Xylocil` | ✔ 名吻合 |

余 3 张：`6e6d14006d8e9514b83a6c99e572081c` Quail · `6ed51d59b9f8ea24ab0dc7ceaef7956c` Duelist's Hubris (Lucius' Talent) ·
`b9665eadf1207ff49bcf0a730b0b7f9d` Euphoric Strike (Lord Exultant's Talent) —— 同样名吻合。

**判据**：`guid_map.tsv` 的 `field=cardImageReference` 行 → 退键 → `uniqueId` ∩ 卡池 id。
⚠️ 真值快照里另有第 24 张 `d0549600681827a42963bd24a46f297a`（Pledge to the Dark Prince），
**它不在我们池里**（我们池里同名同 GUID 的那张 id 是 `EC38`，重复 SO）。
⇒ 另有 **8 条退键音频行**（`EC9`/`EC2`/`EC7`/`EC12`/`EC24`/`EC32` summon + `EC55`/`EC56` death），
8 条的名字**都与卡名吻合**、且 `assets_full/AudioClip/` 里**8/8 实存**（`VO_CSM_EC_Lord Kaphrael_death` 等）。

---

## 3 · 逐张核（退键批次：拿对 / 拿错）

### 3.1 结构判据：只有 2 个键是「两条不同 GUID 抢同一个键」

把 2,342 条 `cardImageReference` 按 `(束, 容器键)` 分组、看**不同 GUID 数**：**只有 2 组**（其余组都是同一 GUID 的重复 SO）：

| 键 | 抢它的两条 GUID | 结果 |
|---|---|---|
| `c6` | `c651ff8d…`(**EC9 退键**) + `c6bb19dd…`(EC56 **exact**) | 都解到 `inf_Icon of Excess Infractor` |
| `f0` | `f0bab801…`(**EC35 退键**) + `f0a86ec5…`(EC60 **exact**) | 都解到 `dae_Blissbringer` |

⇒ **其余 21 张退键卡的 `(束,键)` 没有任何别的卡来抢** + 解出的**资产名与自己的卡名逐字吻合**
⇒ 退键**没有偷别人的图**。（另做一版：把 56 条退键行的「卡名 vs 解出资产名」归一化比 —— **只有 `EC14` 一处字面对不上**，
原因是真值叫 `Varius, Lord Kakophonist` 而资产叫 `inf_Lord Kakophonist`，**§3.2 已开图证实是同一张画**。）

### 3.2 PnP 成品图亲核（`d:/2/Warpforge部队卡片/Emperor_s Children/`）

| 卡 | 开过的图 | 结论 |
|---|---|---|
| **EC9** | PnP `3部队/Warpforge_14_Icon-of-Excess.png` ↔ 资产 `CSM_EmperorsChildren_inf_Icon of Excess Infractor.png` | **同一幅画**（粉肩甲、獠牙面罩、绿眼、爆弹枪、上方带翼徽记、背后火海）⇒ **退键拿对了** |
| **EC56** | PnP `1督军/Warpforge_01_Lord-Kaphrael.png` ↔ 同一张资产 | **不是同一幅画**（PnP 是绿羽缨、金环、紫刃）⇒ 这张的**真值引用是脏的**（同 `AA_A1770.md` 结论）；而它走的是 **`exact=True`** 那条路 ⇒ 🔴 **判错的是 exact，不是退键** |
| **EC35** | PnP `3部队/Warpforge_26_Blissbringer.png` ↔ 资产 `…_dae_Blissbringer.png` | **同一幅画** ⇒ 退键拿对了；同理 `EC60` 那条 `exact` 是脏的（`AA_A1770.md:85` PnP 已核） |
| **EC16** | PnP `3部队/Warpforge_32_Screamer-Kakophonist.png` ↔ 我们导出的 `art_ec16.png` | **同一幅画**（紫色噪音陆战队员 + 管风琴背板）⇒ 拿对 |
| **EC14** | PnP `3部队/Warpforge_37_Varius-Lord-Kakophonist.png` ↔ `art_ec14.png` | **同一幅画** ⇒ 拿对（资产名只是短写法） |

⇒ **逐张判：23/23 「拿对」，0 张「拿错」**。

### 3.3 强制量化旁证（PnP 自动对拍**不可靠**，已如实记）

想用 NCC 自动对拍 PnP，**没做成**：标定窗（EC9）最优 `NCC=0.311`，而**错的 PnP 卡最高 0.230、中位 0.159**
—— 鉴别力太弱（PnP 插图窗被文字面板压住 + 我们导出的图是竖裁、与 PnP 窗宽高比不同）。
⇒ **PnP 那一维只能靠开图亲读**（§3.2），**不拿它当量化判据**。

**可用的量化判据**（我们导出立绘 ↔ 退键解出的资产，两条**互不依赖**的链）：
`NCC(art_<id>.png, 退键资产)` = **0.99–1.00（21/23 张）** ⇒ 两条链**落到同一张图**。

---

## 4 · 我们工程【实际用的】是哪一张（是否已被 `ART_ID_ALIAS` 点名纠正）

### 4.1 `import_original_art.py` **根本不读 `guid_map.tsv`**

现读其认领链：**⓪ `ART_ID_ALIAS`（`:195-271`，24 个键）→ ① 精确子串（`:1288-1319`）→ ② 相似度 ≥0.86（`:1325`/`:1339`）**。
⇒ **退键这条缺陷只在「拿 `guid_map.tsv` 当输入的那批活」里生效**，**不在这条立绘链里**。

### 4.2 23 张退键卡 ∩ `ART_ID_ALIAS` = **只有 1 张（`EC61`）**，且**点名值 = 退键结果，等于没纠**

`ART_ID_ALIAS['EC61'] = 'CSM_EmperorsChildren_strat_Dark Prince´s Throne'`（`:249`）—— 与退键解出的名字**一字不差**。

### 4.3 实测：我们真正用的图 = 退键解出的图（21/23），另 2 张**根本不在退键批里**

`NCC(art_<id>.png, 退键解出的资产)`：**21 张 0.99–1.00**（同一张图）；
**例外 2 张 = `EC56`（0.155）/ `EC60`（-0.034）** —— 这两张的立绘行是 **`exact=True`**、**不在退键批**，
且它们的真值本来就是脏值，**已被 `ART_ID_ALIAS` 点名纠正**（`:230` → `warlord_Lord Exultant`；`:248` → `strat_Tools of Torture`），
P 已核、与 PnP 一致。⇒ 🔴 **退键没有污染我们实际出货的立绘**。

### 4.4 退键值**被原样带进下游产物**的地方（这才是真影响面）

| 消费者 | 现读位置 | 退了键会怎样 |
|---|---|---|
| `工具/gen_card_vfx_by_card.py` → `数据/游戏数据/card_vfx_by_card.json` 的 `byCard[*]._assetRefs` | `:160-166`（`cardImageReference→art`）· `:771-793`（`resolve_so_ref`） | **实测确有**：`EC9/EC56` 的 `art` 都记着 `…Icon of Excess Infractor`、`containerKey='c6'`；**⛔ 没记 `exact` 标志** ⇒ 下游**看不出退没退键** |
| 同上 → **运行时产物** `Resources/card_vfx_by_card.json` | `:1269`/`:1281` **整类丢掉 `_assetRefs`**，只有 `:1306` 把 `audio` 三条捞回运行时 | ⇒ **`art` 不进运行时**；但**退键的 8 条语音进了**（实测 `EC56` 的 `audio` 里有 `VO_CSM_EC_Lord Kaphrael_death`，**对**） |
| `工具/_oneoff_1014_p1/build_picks.py`（P1 id 置换，`:79-82`/`:143-144`） | 把 `cardImageReference` 当**第二钥匙 `sprite`** 交叉验证 | 实测 `_tmp_view/p1_1014/picks.tsv` 194 行里**这 23 张一张都没有**（它们本来就有真值 id）⇒ **零影响** |
| `工具/import_original_audio.py`（`:235`） | 经上面那条 `gen_card_vfx_by_card` 链拿语音资产名 | 8 条退键语音**名字全对**；该文件 `:235` 另记「2 条 GUID 在 guid_map 里就没有」（`GOF2`/`BL47`），与本批无关 |

---

## 没查清

- **没查清 1（根因）**：为什么目录的 `internalId` 与束的容器键对**这 31 个 GUID 不一致**（束里 `'d'` 是 AudioClip、资产在 `'db9'`）。
  **我没去查**：只验到「两者不一致」这个事实；**没验**「是不是目录与束来自不同 build」/「前缀唯一性是不是按不同集合算的」。
- **没查清 2**：**只加载了 EC 那一个束**（因为 56/56 退键行都在它里面，束名来自 TSV 的 `bundle` 列）。
  其余 83 个束**没有逐个加载复核** —— 但「退键行是否只在 EC 束」这一问**由 TSV 列直接答了**（56/56）。
- **没查清 3**：PnP 自动对拍**没做成**（§3.3 已写清为什么、给了实测数字），那一维**只有 5 张是开图亲读**的；
  其余 18 张靠「资产名逐字吻合 + 无第二张卡来抢 + `art`↔资产 NCC≈1.0」三条旁证。
- **搜过的词**：`guid_map`（全仓 `工具/` `资料/`）· `cardImageReference` · `ART_ID_ALIAS` · `summonAudio`/`deathAudio` · `exact` / `startswith` / `containerKey`。
- **读过的文件**：`工具/_guid_resolve_probe.py`（全）· `工具/import_original_art.py`（`:190-271`·`:1240-1320`·`:1325-1340` 邻域）·
  `工具/gen_card_vfx_by_card.py`（`:150-194`·`:742-800`·`:1269`/`:1281`/`:1306` 邻域）· `工具/_oneoff_1014_p1/build_picks.py`（全）·
  `工具/import_original_audio.py`（grep）· `资料/普查产出_第三十一会话/{Q_A1770与A1799判据.md, AA_A1770.md}` ·
  `资料/普查产出_第二十七会话/guid_map.tsv` · `catalog_main.json` · `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json` ·
  `cards_engine.json` · `_tmp_view/p1_1014/picks.tsv` · `数据/游戏数据/card_vfx_by_card.json` · `Resources/card_vfx_by_card.json`。
- **开过的图（共 9 张）**：PnP `Emperor_s Children/3部队/Warpforge_14_Icon-of-Excess.png`（EC9）· `1督军/Warpforge_01_Lord-Kaphrael.png`（EC56）·
  `3部队/Warpforge_26_Blissbringer.png`（EC35）· `3部队/Warpforge_32_Screamer-Kakophonist.png`（EC16）· `3部队/Warpforge_37_Varius-Lord-Kakophonist.png`（EC14）；
  资产 `assets_full/bundle_chaosspacemarinesemperorschildrencardassets_assets_all/Texture2D/` 的 `CSM_EmperorsChildren_inf_Icon of Excess Infractor.png` ·
  `CSM_EmperorsChildren_dae_Blissbringer.png`；我们的 `Resources/Art/cards/art_ec9.png` · `art_ec16.png` · `art_ec14.png`。

---

## 倾向（⛔ 裁定由主对话下）

1. **不构成「静默拿错」**：56 行退键里，**没有一行**能把资产换错 —— **23 张池内卡 23 张都对**；
   两处「抢键」里**错的恰好是 `exact=True` 那一方**（真值快照脏值），**退键那一方是对的**。
2. **但它仍然是真缺陷（该记该改）**：判据（`:198`/`:219`）本身**不发声** —— `exact` 标志**没写进 TSV、也没写进任何下游产物**，
   而唯一线索（键长）**已证不可靠**（假阳 171 / 假阴 26）。⇒ 建议把 **`exact`（或 `internalId`）加成一列**，
   让下游能一眼分辨；⛔ 别按「键短就是退键」改别的地方。
3. **建议就地订正一句文档**：`AA_A1770.md:177` 那句「**2 字符的 `c6`/`f0` 就是退键**」应当改成
   「**`containerKey ≠ 该 GUID 的 `internalId`** 才是退键；`c6`/`f0` 只是这两个实例的**线索**」——
   否则下一个会话会拿它当判据（本仓铁律 5）。
4. **优先级**：本条**不阻塞** `A1799` 的 `fullHeroSpriteReference`（那 59 条是**另一批 GUID**，`guid_map` 里 0/59 命中，
   与本批退键无关）；但 **`fullHeroSpriteReference` 若将来走 `_guid_resolve_probe.py` 重建**，
   ⛔ **必须先把 `exact` 一起落盘**，否则同样分辨不出。
