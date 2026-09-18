# 语音线 —— 原版规格 + ASR 管道（2026-09-18 建立）

> **什么时候看这份**：要接一条语音的触发时机、要动 ASR 管道、或者要查「这条语音什么时候播」的时候。
> 本文件**只留现在还成立的**：原版规格（有出处的）+ 管道用法 + 两个幻觉模式。
> 各家原始产出与逐轮过程在 `资料/普查产出_0913/语音索引.md` 与 `资料/战斗UI_原版对账表.md`。

---

## 〇、一句话现状

**1844 条原版单位语音已进工程**（606 张卡；`Assets/CardPresentation/Resources/Art/audio/vo/`，gitignore）。
**已接的触发 7 族**：`Deploy`(`greet/intro/line`) · `Attack` · `Death` · `Concede` ·
**`CantDo`**（非法操作，14 个拒绝点）· **开局独白 `intro`/`mirror`/`vs*`**（严格先手→后手串行）·
**`ChatPopup` 那 6 个钮**（`Battle/ChatPopupPanel.cs`，2026-09-18 做完，见 §1.7）。
**没接的**：`hurry`（回合时限将到，`timeToHurryUp = 35.0`，每回合一次）。
🔴 **2026-09-18 用全量反编译把「其余各族什么时候播」查清了** —— 见 §1.2。
✅ **英文本转写已跑完**（2026-09-18）：**1844/1844 零缺口** · A 路 WER **0.1627** / CER **0.0770** · **1303 条可自动采信**、624 条进人工队列。产物落 `资料/语音转写_ASR_0918/`，见 §2.5.2。

---

## 一、原版的语音机制（**硬实据**）

### 1.1 `RawCardScript` 上恰好 11 个 chat 音频字段 + 1 个 default

与 `ChatMessage` 的 **11 个枚举值一一对应**（字段名自带 `[FormerlySerializedAs]`，等于自证）：

| 枚举值 | 字段 | 改名前 | 音频文件族 |
|---|---|---|---|
| 0 `Intro` | `introChatSound` | — | `intro` |
| 1 `MirrorMatch` | `mirrorChatSound` | — | `mirror` |
| 2 `Bored` | `hurryChatSound` | `boredChatSound` | `hurry` |
| 3 `ICantDoThat` | `iCantDoThatChatSound` | — | `cantdo` / `cant` |
| 4 `Concede` | `concedeChatSound` | — | `concede` |
| 5 `Greet` | `greetChatSound` | — | `greet` |
| 6 `Threat` | `threatChatSound` | — | `threat` |
| 7 `WellPlayed` | `wellPlayedChatSound` | — | `wp` |
| 8 `Taunt` | `generic1ChatSound` | `tauntChatSound` | `gen1` |
| 9 `Sorry` | `generic2ChatSound` | `sorryChatSound` | `gen2` |
| 10 `Oops` | `generic3ChatSound` | `oopsChatSound` | `gen3` |
| — | `defaultChatSound` | — | 兜底 |

**出处**：`dump.cs:23298-23315`（字段+偏移）· `decomp_full/RawCardScript__GetAvailableSounds.c` · `dump.cs:23189-23207`（枚举）

### 1.2 🔴 各族触发时机（2026-09-18 全量反编译定案）

| 族 | 条数 | 触发时机 | 置信度 |
|---|---|---|---|
| `intro` (0) | 53 | **开局独白**，双方各说一次 | **实据** |
| `mirror` (1) | 53 | 开局独白，但**双方督军相同**时改取这条 | **实据** |
| `hurry` (2) | 54 | 本回合时限将到（原版 `timeToHurryUp = 35.0`，**每回合一次**） | **实据** |
| `cantdo` (3) | 54 | **玩家做非法操作时**（见 §1.3，**14 个触发点**） | **实据** |
| `concede` (4) | 53 | 对局结束 | **实据** |
| `greet/threat/wp/gen1/gen2/gen3` (5–10) | 53×6 | **玩家点 `ChatPopup` 的 6 个钮**；AI 在玩家发言后随机回 **[5,10]** | **实据** |
| **`vs<督军名>` / `vs<阵营名>`** | 206 | 🔴 **不是独立时机** —— 是「**打特定对手（或对手阵营）时的开场白**」，命中就替换 `intro` | **实据** |
| `gen4..gen7` | 207 | ❌ **本版代码里不存在** —— 见 §1.4 | **实据（不存在）** |
| `backup1..6` | 74 | ❌ **全代码 0 引用** —— 见 §1.4 | **实据（0 引用）** |

### 1.3 `cantdo` 的确切链路（**这是可直接实现的**）

```
BattleTipController.NotifyCantDoAction
   → DisplayLocalChatMessage(vlc, 3, skipCanChat=1)      ← 常量 3 只硬编码在这一处
   → DisplayWarlordRegularChatMessage(vlc, 3, true)
   → GetChatMessage(3) + GetChatSoundAsset(3) → 素材字段 0x1B0（空则回落 defaultChatSound 0x188）
```

**闸门**：只在 `IsAnyVoiceLinePlaying() == false` 时插播（**不打断正在说的**）；
`skipCanChat = 1` ⇒ **聊天按钮禁用时、甚至非战斗中也会播**。

**14 个调用点，全部是「操作被拒」**（`decomp_full/BattleManager__*.c`）：

| 触发场景 | 出处 |
|---|---|
| 打不出这张牌 | `CanPlayCard.c:172` |
| 主动技能用不了 | `CanUseActiveAbility.c:130` · `ReadyToUseActiveAbility.c:71` |
| **用不了路标石** | **`CanUseWaystone.c:41`** |
| 攻击目标不合法 | `IsValidAttackTarget.c:260` 和 `:336` |
| 试用主动技能失败 | `TryUsingActiveAbility.c:38` · `TryUsingActiveAbilityOfCard.c:39` |
| 选目标点了个无效的 | `FinishedChoosingAbilityTargetByClick.c:27` |
| 格挡等待中做了别的 | `WaitingForBlockingAction.c:132` |
| 回合没走完就点结束 | `EndTurnClick.c:91` |
| 另两处 | `Update.c:672` 和 `:1212` |

⚠️ 我们工程里对应的「拒绝点」在 `CardPresentation/Hand/CardInteraction.cs` 与 `Battle/BattleDriver.cs`，
**不是** `BattleManager` —— 接的时候要找我们的对应物，别照抄类名。

### 1.4 两族「查不到」已定案为「不存在」

- **`gen4..gen7`（207 条）= 原版随包发出、代码引用不到的死素材。**
  `GetAvailableSounds()` 新建长度 **15** 的数组：第 0 格 `defaultChatSound`、**第 1–11 格是那 11 个 chat 字段**
  （第 11 格 `generic3` 就是**最后一个**）、第 12–14 格是 `summonAudio` / `attackAudio` / `deathAudio`。
  `generic4..7ChatSound` **在字段表里根本不存在**（类在 `0x1E0` 结束）；`GetChatSound` 只 `case 0..10`，
  越界会打 `"No chat sound defined for chatMsgIndex "` 再退回 default。
- **`backup1..6`（74 条）= 录音交付件。** `backup` 全库只 2 处、都是账号功能；最大编号 **6 不是 12**、还有断档
  （Ghallaron 从 Backup2 起 / Saffa Rhiannor 只有 Backup4 / Gordrang 有 1/2/3/4/6 缺 5），另有 2 个无编号的。
  **决定性证据**：文件名里直接写着台词 —— `VO_SpaceWolves_Terminator Rune Priest_backup- I bring the storm-wrath!.ogg`。

### 1.5 开局独白的规格（`intro` / `mirror` / `vs*`）

- **严格「先手 → 后手」串行**：先手那条**播完**（`yield WaitForSeconds(GetCurrentClipLength())`）才播后手。
- **除「等当前语音播完」外没有任何额外秒数**，无随机、无计数。
- 期间 `popUpEnabled = false`（`CanChat` 会拒），**全部播完才置回 true**。
- **教程局 / campaign boss 局根本不 StartCoroutine**（不播 intro）；campaign boss 那条协程只做
  `SetPopUpDisabled → ExecuteCampaignScriptedTurn → WaitForSeconds(常量) → SetPopUpEnabled`，**不播任何语音行**。
- `vs*` 的查表**不在协程里**，在 `ChatManager`：`GetCustomIntro(己方督军, 对方督军)` →
  失败 `GetCustomIntroByArmy(己方, 对方.army)` → 再失败回落 `RawCardScript.GetChatSound(warlord, 0)`。
- 数据来源：`RawCardScript.customIntroChatData // 0x1E8`（`opponentWarlord` + `customSound` + `talksFirst`）与
  `customIntroByArmyChatData // 0x1F0`（`army` + `customSound`）。
  🔴 **2026-09-18 更正：这里原来写「文件名本身就带对手名/阵营名，映射可以从文件名推」—— 实测不成立。** 见 §1.5.1。

### 1.5.1 `vs*` 的对手名 —— **对照表已建成**（2026-09-18）

**原设想**：文件名带对手名（`VO_AM_Ursula Creed_vsTyranids.ogg`）⇒ 映射可以从文件名推。
**第一步实测：靠自动匹配推不出来。第二步：改走「查表」，表已经建好了。**

#### a) 🔴 先更正一条我说错的：「两套词汇」不成立

我一度断言「`vs` 用**军团名**、我们的 `faction` 是**子阵营名**，两套词汇对不上」。**错了。**
实测原版**只有 `CardArmy` 这一个枚举**（`D:/2/tools/il2cpp_out/dump.cs:45634`）：

```
Neutral=0  Ultramarines=10  Goff=20  SaimHann=30  Sautekh=40  BlackLegion=50
Leviathan=60  TauEmpire=70  Sororitas=80  Genestealers=90  AstraMilitarum=100
DarkAngels=110  EmperorsChildren=120  SpaceWolves=130
```

⇒ **我们 `voice_lines.json` 的 13 个 `faction` 就是 `CardArmy` 本身**；`grep` 全表**没有** `Legion`/`Race`/`FactionGroup` 这类枚举。
所谓「父军团」**不在一层枚举里，是资源包命名给的** —— bundle 名 = `<父军团><子阵营>cardassets`：

| bundle 名 | 父军团 | 我们的 faction |
|---|---|---|
| `aeldarisaimhanncardassets` | Aeldari | SaimHann |
| `chaosspacemarinesblacklegioncardassets` | Chaos Space Marines | BlackLegion |
| `chaosspacemarinesemperorschildrencardassets` | Chaos Space Marines | EmperorsChildren |
| `necronssautekhcardassets` | Necrons | Sautekh |
| `orksgoffcardassets` | Orks | Goff |
| `spacemarinesdarkangelscardassets` | Space Marines | DarkAngels |
| `spacemarinesspacewolvescardassets` | Space Marines | SpaceWolves |
| `spacemarinesultramarinescardassets` | Space Marines | Ultramarines |
| `tauempirecardassets` | Tau Empire | TauEmpire |
| `tyranidsleviathancardassets` | Tyranids | Leviathan |
| （其余 3 个 1:1） | Astra Militarum · Genestealer Cults · Adepta Sororitas | AstraMilitarum / Genestealers / Sororitas |

⇒ **10 个军团 → 13 个 `CardArmy`，只有两个军团是多对一**：
**Space Marines**（DA/SW/UM 三个）与 **Chaos Space Marines**（BL/EC 两个）。其余 8 个是 1:1。

#### b) 素材侧 `ev` 的命名确实很乱（所以不能拼字符串）

| 现象 | 例子 |
|---|---|
| 只取名字的**前段** | `vsursula`（`Ursula Creed`）· `vscalgar`（`Marneus Calgar`）· `vsabaddon` |
| **阵营缩写** | `vsum` · `vssm` · `vsnl` · `vstbc1..4` · `vsts` · `vswe` · `vsdg` |
| **撇号变体** | `vsaun'va` = `vsaunva` · `vso'maisos` = `vsomaisos` |
| **单复数混用** | `vsork` / `vsorks` · `vsnecron` / `vsnecrons` · `vstyranid` / `vstyranids` |
| **原版自己拼错** | `vsastramillitarum`（多一个 `l`） |
| **合并键** | `vsec&bl`（EC+BL）· `vsorkstyranids`（Orks+Tyranids）· `vsasmodai&belial` · `vsleontus-ursula-sm` |

**实测的自动匹配率**（99 个 token）：前缀 16/99 · 包含 69/99 · 去复数 71/99 · **我们采用的安全判据（短词⊂全名 + 长度≥4）57~59/99**。
- **方向不能反**：`vscalgar` → `calgar`，要 `marneucalgar.Contains("calgar")`。
- **护栏不能少**：不设长度下限，`vsda`→`da` 会**同时命中** `DarkAngels` 与 `Ae**lda**ri` ⇒ **播错语音**。
  按「宁可认不出，不可认错」，**短于 4 的一律不认**。

#### c) ✅ 于是改走查表 —— **99 个 token 逐条裁定完了**

| 档 | token | 行数 |
|---|---|---|
| **实据**（卡池/PnP 卡图/`CardArmy`/bundle 名交叉核过） | **76** | 173 |
| **推断**（战锤设定，已标注） | 6 | 8 |
| **认不出**（**不硬塞**） | **17** | 25 |

🔴 **关键：值是集合，不是单值。** 有 **6 个 token 覆盖多个阵营** —— `vssm`/`vsspacemarine`（DA+SW+UM）·
`vscsm`/`vschaos`（BL+EC）· **`vsec&bl`** · **`vsorkstyranids`**。
⇒ 老那套「一个 token 一个阵营」的模型**根本表达不了这些**，`Core/VoiceLines.cs` 的 `VsFactionTokens`
**已经改成按阵营查、值是 token 集合**。

**认不出的 17 个**（**搜过、证过，不是没找**）：
`vstbc1..4` = 原版自己的 **To Be Confirmed 占位符** ·
`vsdg`/`vsts`/`vswe`/`vsnl`/`vsdrukhari`/`vshesperax1..2`/`vsmortarion`/`vsmkar` = **未实装的军团** ·
`vsdaemon`/`vsimperial`/`vsknighttitan`/`vsleontus-ursula-sm` = 泛称或多合一。

#### d) 「原版那张权威表不在本地」—— 独立复核过（带对照组）

`CustomIntroChatData` / `CustomIntroByArmyChatData` 是 `RawCardScript` 上的字段（`dump.cs:23316-23317`，偏移 `0x1E8`/`0x1F0`）
⇒ 表挂在**卡牌资产**上。实测（**带对照组，排除「工具没读进去」这个伪结论**）：

```
rg -l "customIntro"      assets_full  → 0 命中
rg -l "opponentWarlord"  assets_full  → 0 命中
rg -l "deckHero"         assets_full  → 236 命中      ← 对照组：说明 rg 能读
```

#### e) 🔴 两条我们原来记错的事实（都改进了代码注释）

1. **`vs*` 是「只说对手是谁」，不是「谁对谁说」** —— 三条独立证据：
   ① 同一 token 出现在**多个不同主讲**下（`vstyranids` 有 **9 个**主讲）；
   ② 同一对对手**双向各录一条**（`Asmodai_vsBelial` ↔ `Belial_vsAsmodai`）；
   ③ **236 个文件名逐条扫，0 个反例**（没有任何文件的 `vs` 后缀等于它自己的主讲名）。
2. **主讲不只限督军** —— `Hound of Abaddon` / `Ghallaron's Champion` / `Acolyte Iconward` /
   `Celestian Sacresant Aveline` 这些**普通单位也在说 `vs*` 开场白**，
   因为 `customIntroChatData` 挂在 `RawCardScript`（**所有卡**）上。
   ⇒ **别把这条链只接在督军身上。**

#### f) ✅ 顺手查出的缺口 —— **2026-09-18 当天修完了**（**原来是它记错了**）

> 🔴 **更正**：这一节原文写的是「`_vs` 文件漏了 **11** 个（全在 Necron 包）、**未修**，只报告」。
> **那只是冰山一角，而且是错的量法。** 实测缺口 = **70 条音频没进表**，其中**三张卡整条语音线丢了**。

**真实缺口与根因**（`import_original_audio.py --check` 实测）：

| 病 | 现象 | 影响了多少 |
|---|---|---|
| ① **卡名是美术文件名尾段，不是卡名** | `SAU1` 的 `name` 写作 **`stormlord`**、`SAU5` 写作 **`Diviner`**（`card_stats.json` 的 `ocrName` 里真名都在），而音频文件名用的是真名 | **45 条**（Imotekh 22 + Orikan 23）—— 整条线 |
| ② **join 键是 `(阵营, 卡名)`、两侧都没 `strip`** | `card_index.json` 是**改名之前**建的（`Land Rider`/`Sister Dogmata`/`Morkai Eliminator`/`' Iron Priest'`），卡表里已是新名 | 8 张卡 join 不上 |
| ③ **孤儿路径判据太窄** | 只认 `_卡名_` 与「以 `_卡名` 结尾」；而 **481 条**文件名是 `VO_<阵营>_<卡名> - <台词>` 形状，名字后面跟的是 ` - `。另有**撇号变体**（卡面 `Bone’ead` U+2019 vs 文件名 `Bone'ead`） | 约 20 条 |

**修法（三处，都走正规通道）：**

1. **卡名** → `gen_cards_engine.py` 的 `STAT_FIXES` 加两条 `name` 覆盖；
   同步改 `数据/游戏数据/card_ids.json` 的 `SAU1`/`SAU5`（**不改这两行会掉成自造 id**，因为 id 是按名字查的）。
   三条独立证据 + PnP 编号三方吻合：PnP 卡面 `Warpforge_1_Imotekh-the-Stormlord.png` / `Warpforge_5_Orikan-the-Diviner.png`
   · `card_stats.json` 的 `ocrName` · 美术路径 `Necron_Sautekh_warlord_Imotekh the Stormlord_AA_HB.png`。
   顺带把 `ZH_NAME_ALIAS` 补两条（中文表里还留着旧键）；**立绘不用重导**（按 id 命名）。
2. **join 容错** → 两侧 `.strip()`；再加一张**改名别名表**，**直接从 `gen_cards_engine` 的
   `STAT_FIXES` + `ZH_NAME_ALIAS` 读**（不另抄一份 —— 抄第二份迟早不一致）。
3. **孤儿判据重写** → `_norm`（撇号统一/小写/标点转空格）+ `match_owner`（**整词边界**，
   只拿 ` - ` 之前那段比 —— 台词里会出现**别的卡的名字**，拿整串比会误配）。
   允许**卡名首段**当短名（`VO_Sautekh_Imotekh_*` ← `Imotekh the Stormlord`），
   但**必须过唯一性护栏**（首段在池里只指向这一张卡）—— 否则不认（宁可认不出，不可认错）。

**结果**：台词 **1787 → 1844**（+57）· 有语音的卡 **595 → 606** · 未并上的孤儿 **70 → 13**。

**剩下 13 条残差（逐条核过，**不硬塞**）：**

| 类别 | 条目 |
|---|---|
| **原版自己的占位符**（`To Be Confirmed`） | `Goff_TBC` · `Sautekh_TBC`×3 · `UM_TBC1` |
| **池里没有这张卡** | `SaimHann_Extra10` · `Tau_Empire_Aun'Shi` · `EC_Threnodic Choir Flawless Blade` |
| **写法差异，不够格认** | `Goff_Trukkboy`（卡名 `Trukk Boy`）· `Tyranid_Leviathan_ScreamerKiller`（`Screamer-Killer`）· `UM_Firestrike Servoturret`（`Firestrike Servo Turret`）· `UM_Invader ATV`（池里叫 `Primaris Invader`） |
| **不带事件尾段的原版遗留** | `BL_Ghallaron_Backup1 (vsAeldari)` · `CSM_EC_KonstrictusTormentor (Andrew)` |

**顺带多出 4 个 `vs` token**（随那 45 条一起回来的）：
`vstrazyn`→Sautekh · `vscadia`→AstraMilitarum（**推断**）· `vstbc`→认不出 · 以及 `vsorikan`/`vsimotekh` 的条数各 +1。
**它们目前还没进 `VsFactionTokens`**（那张表只收「阵营级」的），要不要收是**另一次裁定**。

#### g) ⚠️ 一条工具教训

**`D:/2/tools/all_strings.txt` 里只有方法名/类型名，没有内容字符串。**
一批剧情词（`Tyranids` / `Necrons` / `Mortarion` / `Hesperax` / `Trazyn` / `Agemman` …）在里面**全部 0 命中**。
⇒ 之前几轮把它当「全量字符串表」用过，**那个用法是错的**（它不能用来否证内容层的东西）。


### 1.6 🔴 泰伦虫族：那一族**大量是虫叫，不是台词**

实测 `VO_Tyranid_*` 里带 `- <描述>` 的共 **33 个，33 个全部是叫声/动作描述**
（`Primal roar` / `Thunderous stomp` / `Rising talons menacingly` / `Viscous gurgle` / `Pack call` …），**没有一个字是台词**。
对照：CW 48 个带描述里只有 7 个是音效；其余阵营 0–12%；**全库 481 个带描述里 49 个是音效（10%）**。

⇒ `工具/asr/wer.py` 里有一条**按阵营的硬判据** `NON_SPEECH_DESC_FACTIONS = {'tyranid'}`，不靠词表猜。

#### 🔴 1.6.1 这批虫叫**在原版里当什么用**（2026-09-18 定案）

**结论：既不当气泡文字、也不是气泡内容 —— 它们是「普通单位卡的出场语音」。**

- 原版的**气泡（聊天）系统只服务督军**；单位出场那条路要求**本地化词条非空**，
  **取不到就整个气泡不出现**，只打一行 `LogError("Missing localized message for deploy sound for " + 卡名)`。
- **文件名里 `Primal roar` / `Thunderous stomp` 这类描述，从来没有任何代码路径当文字用过。**
- **两条路行为不同**（写代码时别混）：
  | 路径 | 空文本时 |
  |---|---|
  | **督军聊天**（`DisplayWarlordRegularChatMessage` → `VoiceLineBox._ShowChat`） | **没有空值保护** ⇒ 仍会出现「**有立绘、有声、无字**」的气泡 |
  | **单位出场**（`DisplaySummonMessage`） | **显式 `IsNullOrEmpty` 门** ⇒ **直接 return，气泡不出现** |
- **没有阵营差异**：`Tyranid`/`Leviathan` 在全部 `ChatManager__*.c` / `VoiceLinesController__*.c` 里 **0 命中**。
- **文字的真源头是 I2 本地化词条**：`GetChatMessage` → `RawCardScript.GetLocalizedChatMsg` → 查 `Chat_Messages/<chatMessagesReferenceId>[-Chat<idx>]`。
  🔴 **词条值本地没有、而且不在客户端** —— 原版 I2 表在远端 `localization_assets_all.bundle`（CCD，已关服）。
  实测：游戏目录 + 84 个 bundle 搜 `Chat_Messages` **0 命中**，而同目录别的字符串（如 `Neurothrope`）读得到 ⇒ **表根本不在客户端**。
- **旁证**：原版自家数据里写的是 `PlayerChat (*Primal roar*)` —— **星号包起来的舞台指示体例**
  （`素材/Warpforge原版/游戏数据/教程/MonoBehaviour/Warpforge_TutorialStage6.json:2809`）。

**⇒ 我们该怎么做**：那 33 条的 `text` **留空**（原版就是只播音不显字），
文件名那段描述当**备注**（`source = non-speech`），**不当气泡文字**。
⚠️ 顺带更正一条**不成立的推理**：不能拿「`zh_CN.csv` 里 0 命中」去证明「原版没有 chat 词条」——
`zh_CN.csv` 是**我们自己的串表**（键 = 英文原文），**不是原版词条表**。

**另外**：`gen4..gen7` 在泰伦这边**只有 3 位督军有**（Neurothrope / SwarmLord / Tervigon，各 17 条），
普通单位一位都没有 —— 与 §1.4「207 条是死素材」完全自洽。
它们是虫叫还是台词：**ASR 两路都判非人声**（`gen4/5/6` 转写空或被标 halluc/echo，`Swarmlord_gen7` → `Roooooooo!`），
但 ASR 是**弱证据**；**强证据只有「代码永不播放它们」**。

### 1.7 `ChatPopup`（**唯一还没接的那一族**）—— 规格已齐，只差面板

原版是**玩家点 6 个钮**发话（枚举 5…10），AI 在玩家发言后随机回 **[5,10]**。

**入口（🔴 2026-09-18 更正）**：**就是 `ChatButton` 自己** ——
`ChatButton`（GameObject 85）身上的 `EverguildButton`（MB 5291）`m_OnClick → BattleManager.ClickChat`
→ `VoiceLinesController.ClickChat` → `VoiceLinesPopupSelector.Clicked`（开关）。
它的父链是 **`PlayerInfo` → `LeftArea`**（RT 2984 → 3319 → 2849），**不在 `EnemyInfo` 下**
（`EnemyInfo` 那侧的 `m_Calls` 是空数组）。`VoiceLinesController.chatButton` 也指向同一个 5291 —— 4 秒冷却就挂在它身上。

#### 🔴 1.7.0 `ChatButton` 上**挂着两个组件** —— 两条旧结论其实都有出处，别删任何一条

| 组件 | 实据 | 我们怎么处置的 |
|---|---|---|
| `Button`（`EverguildButton`）**MB 5291** | `m_OnClick → BattleManager.ClickChat`（2026-09-18 亲读 `NEW/MonoBehaviour/MonoBehaviour_5291.json`） | ✅ **接了**（开 `ChatPopup`） |
| `PlayerStateToggle` **MB 4089** | `defaultState=1, status=0, **selectedBool='EnableWarlordVOs'**`（2026-08-27 `子代理读报_back左区_0827.md:59,159`） | ⏸ **没接** —— 待实况确认 |

> ⚠️ **一段被推翻的旧话**：`子代理读报_back左区_0827.md:221` 写的是
> 「它控制 `EnableWarlordVOs`（敌语音开关），**与 FrontCanvas Unit Chat 的 6 个聊天气泡按钮完全无关，勿混**」。
> **后半句不成立** —— 同一个对象上的 `m_OnClick` 确实指向 `BattleManager.ClickChat`，而那条链的终点
> 就是那 6 个钮的面板。**结论：两件事都挂在同一个按钮上**（开面板 + 语音开关），
> 至于**运行时到底哪个生效、还是两个都生效**，静态数据判不了 ⇒ **按铁律 4 跑一次实况**。
> 📌 这条也说明「**一个 GameObject 只干一件事**」是个会害人的假设。

> ⚠️ **原写「`ChatButton` = 敌方语音开关、开面板走 `EnemyInfo`」—— 两半都错**（面板入口那半）。
> 「敌方语音开关」另有出处（上表第 2 行），但**不在这个按钮唯一的作用里**。

| 元素 | 数值 | 出处 |
|---|---|---|
| 面板 | **815.04 × 475.47** · anchor (0, 479.81) · pivot (0, 0.5) · `m_IsActive = false` | 解包场景 `battlearena1` RT 2688 |
| 台词钮 ×6 | 各 **603.60 × 48** · fs30（`autosize` 4–30）· 纵排 `spacing = 1.48` | 同上 |
| 底色 | `White Square` 深绿 `(0, 0.07, 0, 1)` · **557.3 × 301.3** | 同上 |
| 边框 ×4 | `40k_UnitChat_Background_{Top,Bottom,Left,Right}` | 同上 |
| **开面板时长** | **0.5 s**（字段 `timeToOpen`）+ `DOFade(α: 0→1, 0.5)` · 缓动 **`OutCubic`(9)** | MB 4231 |
| **关面板时长** | **0.5 s**（字段 `timeToClose`）+ `DOFade(α: 1→0, 0.5)` · 同上 | MB 4231 |
| ⚠️ **没有水平滑动** | `Awake` 里 `outPos = originalPos − (1.5f × originalPos.x, 0)`，而序列化 `x = 0` ⇒ **显示位与隐藏位同为 (0, 479.81)**，看得见的只有 alpha 0↔1 | `__Awake.c:19-30` |
| 按钮兜底文本 | 6 个 TMP 的 `m_text` **全序列化成 `Greetings`**（取不到本地化词条时屏上就是这个） | 6 个 TMP 组件 |
| 冷却 | **4 s**（`VoiceLinesController.CHAT_INTERACTABLE_COOLDOWN = 4f`；`chatButton.interactable` false→等 4s→true） | `VoiceLinesController.cs:232` |
| 关面板 | `CloseChatPopup`（**父 = `ChatPopup`**，4055.34 × 2114.44）上的 `EventTrigger` `eventID 2 (PointerClick)` → `VoiceLinesPopupSelector.Hide` | MB 4913 |

#### 1.7.1 逐节点绝对矩形（2026-09-18 按 pathid 逐级下推算出来的）

**基准**：Canvas `RT2759` 的 `CanvasScaler` 是 `m_UiScaleMode=1`（ScaleWithScreenSize）·
`m_ReferenceResolution=(1920,1080)` · `m_ScreenMatchMode=1`(MatchWidthOrHeight) · `m_MatchWidthOrHeight=0`
⇒ 1920×1080 下 `scaleFactor = 1`，**1 canvas 单位 = 1 px**，Canvas 矩形 = (0,0)-(1920,1080)（y 向上）。
下表 y 已翻成**左上原点**（`y_top = 1080 − y_up`）。链：`Canvas → FrontCanvas → Safe area FrontCanvas → Unit Chat → ChatPopup`，前四级都是全屏。

| 节点 | 父 | pivot | anchorMin/Max | anchoredPosition | sizeDelta | **绝对 (x1,y1,x2,y2)** |
|---|---|---|---|---|---|---|
| **ChatPopup** RT2688 | Unit Chat | 0,0.5 | (0,0)-(0,0) | (0,479.81) | (815.044,475.47) | **0.00, 362.45, 815.04, 837.92** |
| CloseChatPopup RT3305 | ChatPopup | 0.5,0.5 | (0.5,0.5)-(0.5,0.5) | (2.45,41.66) | (4055.34,2114.44) | −1617.70, −498.69, 2437.64, 1615.75 |
| ChatButtons RT3168 | ChatPopup | 0.5,0.5 | (0,0)-(0,0) | (371,206) | (722.66,403.13) | 9.67, 430.36, 732.33, 833.49 |
| **BG** RT2667（`White Square` 那块） | ChatButtons | 0.5,0.5 | (0.5,0.5)-(0.5,0.5) | (28.816,5.615) | (557.314,301.346) | **121.16, 475.64, 678.47, 776.98** |
| BGFrame RT2976（四条边的容器） | ChatButtons | 0.5,0.5 | (0.5,0.5)-(0.5,0.5) | (2.2221,−1.572) | (716.882,399.1) | 14.78, 433.95, 731.66, 833.05 |
| ├ Top RT2966 | BGFrame | | | (16.3718,171.305) | (517.687,33.923) | **130.75, 445.23, 648.44, 479.15** |
| ├ Bottom RT2729 | BGFrame | | | (17.1093,−153.833) | (517.687,36.532) | **131.49, 769.06, 649.17, 805.60** |
| ├ Right RT3186 | BGFrame | | | (317.0938,13.59) | (83.65,371.064) | **648.49, 434.37, 732.14, 805.44** |
| └ Left RT2602 | BGFrame | | | (−300.237,−5.407) | (117.321,386.982) | **14.32, 445.41, 131.65, 832.39** |
| Buttons RT3449（布局组） | ChatButtons | 0.5,0.5 | (0.5,0.5)-(0.5,0.5) | (8,2) | (557.31,296.06) | 100.35, 481.89, 657.65, 777.95 |
| 6×ChatButton **运行时**（布局组排完） | Buttons | | | | (603.6,48) | x 恒 **77.20 … 680.80**；y：**481.89 / 531.37 / 580.85 / 630.33 / 679.81 / 729.29**（每个 +49.48） |
| ├ bg（子节点 ×6） | ChatButton | 0.5,0.5 | | (23.724,0) | (550.405,44.5) | 钮内 x+23.724；第 1 个钮上 127.52, 483.64, 677.92, 528.14 |
| ├ Text (TMP,×6) | ChatButton | 0.5,0.5 | (0,0)-(1,1) | (26.71,0.0001) | (−74.902,0) | 第 1 个钮上 141.36, 481.89, 670.06, 529.89 |
| └ button（图标,×6） | ChatButton | 0.5,0.5 | (0,0.5)-(0,0.5) | (23.5,0) | (40,40) | 第 1 个钮上 80.70, 485.89, 120.70, 525.89 |

⚠️ **6 个钮的「序列化态」全是 `anchoredPosition = (0,0)`、全叠在容器左下角** —— 真正的位置是 **`VerticalLayoutGroup` 运行时排出来的**。
**布局组参数**（`Buttons` RT3449 上）：`m_Spacing = 1.48` · padding 全 0 · `m_ChildAlignment = 1 (UpperCenter)` ·
`m_ChildControlWidth/Height = 0` · `ForceExpand` 全 0 ⇒ **只定位、不改尺寸**。
自洽核：6×48 + 5×1.48 = **295.4**，容器高 296.06（余 0.66 留底）；宽 603.6 > 容器 557.31 ⇒ **左右各溢出 23.15**（不是居中装不下的 bug，原版就这样）。

**4 条 BGFrame 的图**（`m_Sprite` pathid → 名字用 SpriteAtlas 的 `m_PackedSprites`/`m_PackedSpriteNamesToIndex` 对齐验证过）：

| 条 | pathid | 原生尺寸 | 摆放/原生 |
|---|---|---|---|
| Top | −7266636332286626103 | **590×39** | 0.877× |
| Bottom | 6130416589180386937 | **590×42** | 0.877× |
| Right | 556259315706983459 | **95×427** | 0.881× |
| Left | 450114720226241101 | **134×445** | 0.875× |

四条都是 `m_Type=0`(Simple)、`m_PreserveAspect=0`、`m_Color=(1,1,1,1)`、`m_RaycastTarget=0`，**近似等比 0.874×**。

**底色那块（BG RT2667）**：`m_Sprite = −3734815765473162916 = White Square`（**8×8**，靠 tint 染色），
🔴 **`m_Color = (0.001554, 0.066038, 0.000000, 1.0)`** —— 我先前记的「`(0, 0.07, 0, 1)`」是**凑整过的近似**，精确值是这三个数。

#### 1.7.2 两个「静态数据定不死、要跑实况」的点（**别当已知**）

1. **`TransformScalerBySmallScreenUI`（`menuScale = 1.3`）** —— 面板本体挂着它。
   只在 `menuScale != 1` **且** 静态开关 `GameStaticData.DefaultSmallScreenUI` 为真时 `enabled = true`，
   届时 `LateUpdate` 把 `transform.localScale × 1.3` ⇒ **整棵子树绕 pivot (0, 479.81) 放大 1.3 倍**。
   那个开关是**玩家图形选项**（`GraphicsTab.SmallScreenToggleClick`），**静态数据判不出 1920×1080 下是开是关**。
2. **`UISafeAreaManager`**（Canvas 上）的 `m_safeZones` 含 `Safe area FrontCanvas`（`applyWidth=1, applyHeight=0`）
   ⇒ 设备有**横向安全区**时整个 x 会内缩。**1920×1080 桌面安全区 = 全屏**，上表成立。

> 🔴 **2026-09-18 第二次更正 —— 我把一次「更正」改错了，这里改回来**：
> `CloseChatPopup` 上挂的图形**不是 `Image`，是 `NonDrawingGraphic`**（UnityEngine.UI.Extensions）——
> **不绘制、只吃射线**。所以**它确实是隐形的**（我**最初**那条记录「隐形遮罩」**是对的**）；
> 我随后看到 `m_Color.a = 1.0` 就下结论「不是隐形」，**那次更正才是错的** ——
> 隐形的原因不在 alpha，在**它根本不画**。
> ⚠️ 教训：**「这个字段是这个值」推不出「它的行为是这个」** —— 要知道挂的是哪个组件。
> 我们工程里没有 `NonDrawingGraphic`（那是 UIExtensions 的），实现时用 **1×1 全透明图 + `raycastTarget = true`** 等效。

**6 个钮的音频**已经全通（`VoiceLines.ForChatButton` = `greet/threat/wp/gen1/gen2/gen3` ↔ 枚举 5…10，
`SetupChatOptions.c:54,112` 与 `RefreshChatLines.c:71` **两处独立证实**起点写死 5、按下标 +1 —— **不是随机抽 6 条**）。

🔴 **资源（2026-09-18 已解决）**：四张边框图与 `White Square`
**原先不在 `Resources/`** —— 但**在备查库 `素材/Warpforge原版/UI图集/图集/battleatlasui/sliced/` 里好好的**，
是**没同步**（和 `Card_Frame_Cost_Icon` / `40K_display` 同一个坑）。
已加进 `工具/sync_battle_ui_art.py` 并跑完（**5 张**）。
原图尺寸与落地比例（`资料/战斗规格/战斗重建_0827/子代理读报_front弹层_0827.md:186-189` 实测）：

| 张 | 原图 | 实际渲染 | 比例 |
|---|---|---|---|
| Top | 590×39 | 517.7×33.9 | 0.877× |
| Bottom | 590×42 | 517.7×36.5 | 0.877× |
| Left | 134×445 | 117.3×387.0 | 0.875× |
| Right | 95×427 | 83.7×371.1 | 0.881× |

> ⚠️ **更正我自己上一条**：我一度说「`战斗UI_原版对账表.md:113` 写着这四张『已进工程』」——
> **读错了**。那句原文只说「它们属于 `ChatPopup` 的边框」，**没有**说进没进工程。已撤。

---

## 二、ASR 管道（为什么这么设计）

### 2.1 两路，故意做成「能互相证伪」

| | 通路 A | 通路 B |
|---|---|---|
| 引擎 | `faster-whisper` 1.2.1（CTranslate2，CPU int8） | `pywhispercpp` 1.5.1（whisper.cpp，ggml） |
| 模型 | `small`（461 MB） | `large-v3-turbo-q5_0`（547 MB） |
| **prompt** | **带**逐条微型 prompt | **完全不带** |
| 解码路径 | PyAV 直读 `.ogg` | 先用 PyAV 转 16k WAV |
| 实测速度 | **1.78 s/条**（1787 条按此算 ≈ **53 分钟**；**A 路跑完全量的实测总耗时是 58.7 分钟** —— 差额是模型加载与去 VAD 重试） | **7.01 s/条**（全量 ≈ 3.5 小时） |

**价值**：两路的引擎 / 解码路径 / 量化 / prompt 全独立 ⇒ **只有 A、B 各自独立写出同一个专名才可采信**；
A 写出了卡名而 B 写的是音近的别的东西 ⇒ **那是 prompt 在编**。

**不需要 ffmpeg**：`av`（PyAV，自带解码器）已经在 py312 里。
**必须走镜像**：`huggingface.co` 在本机不通（HTTP 000），要 `export HF_ENDPOINT=https://hf-mirror.com`；
`github.com` 也不通 ⇒ `whisper.cpp` 官方 exe 那条路走不了，但 PyPI 上 `pywhispercpp` 有 `cp312-win_amd64` wheel。

### 2.2 🔴 两个幻觉模式（**不知道就会把胡说当成结果**）

本机 1–4 秒的短音频，在 Whisper 固定的 **30 秒窗口**里 padding 占 **85–95%** —— 这是幻觉高发区。实测两个模式：

| 模式 | 长什么样 | 什么时候出现 | 处置 |
|---|---|---|---|
| **① prompt 回显** | `Voice line of Ghazghkull, Goff.` | **带** `initial_prompt` 时，模型**抄 prompt** | 清空文本 |
| **② 训练样板话** | `Thank you for watching!` · `Thanks for watching!` · `You` | **不带** prompt 时，模型抄训练语料里的常见短语 | 清空文本 |

⇒ **两条路在非人声文件上都会胡说**。所以 `repairA.py` 的「去掉 prompt 重转」**不算修好**（它只是把①换成②）；
**正确处置是「判成非人声、清空文本」**，由 `工具/asr/clean.py` 统一做。
**中招率**：A 路全量 1787 条里清掉 **46 条回显 + 33 条样板话 = 79 条（4.4%）**。

### 2.3 判据：用「文件名里自带台词」的那批当靶子

**481 条**的文件名形如 `VO_<阵营>_<单位> - <台词原文> (配音).ogg` —— 那是**现成的对照集**。
扣除舞台指示与虫叫后，参与算分的约 **250 条**。

- **WER 与 CER 并列**：专名拼错（`Ghazghkull` vs `Gaskull`）在 WER 里算 1 个错词、CER 里只算几个字母 ⇒ 两者并列才能分清「整句没听清」与「就差拼写」。
- **语料级口径**：WER = **Σ编辑距离 / Σ参考词数**（**不是各条的平均** —— 短句会主导平均）。
- 已知的**系统性弱项**：**兽人方言**（`Youz likes it hot` → `You rascals!` · `Make Gork proud boyz` → `Michael Pratt Boys`）和**短促呐喊**（`For Cadia!` → `Fuck Katia!`）。

### 2.4 怎么跑

```bash
PY=D:/2/Warpforge_tools/py312/python.exe
export PYTHONIOENCODING=utf-8 HF_ENDPOINT=https://hf-mirror.com HF_HOME=D:/2/tools/asr/hf

$PY d:/2/tools/asr/runA.py                 # 通路 A（带 prompt）→ passA_fw-small.tsv
$PY d:/2/tools/asr/clean.py  <passA.tsv>   # 清两种幻觉（就地改写，另加 clean_flag 列）
$PY d:/2/tools/asr/repairA.py <passA.tsv>  # 只对中招行去 prompt 重转（可选，见 2.2 的警告）
$PY d:/2/tools/asr/runB.py                 # 通路 B（无 prompt）→ passB_wcpp-turbo.tsv
$PY d:/2/tools/asr/clean.py  <passB.tsv>
$PY d:/2/tools/asr/compare.py <passA.tsv> <passB.tsv>   # 合成对照 + sidecar
$PY d:/2/tools/asr/wer.py     <passA.tsv> [passB.tsv]   # 只算准确率
```

**工具放 `d:/2/tools/asr/`**（守铁律 8：别往 `d:/4/` 堆）。
**装法**：`pip install faster-whisper==1.2.1 pywhispercpp==1.5.1` —— 实测**只新增 14 个包、不动任何现有包**
（装前先 `pip install --dry-run` 验过）。⚠️ `D:/2/Warpforge_tools/py312` 是**精简版 Python、没有 `venv` 模块**，所以就地装。

### 2.5 产出（**F 的决定**）

全部落 **`d:/4/Unity/资料/语音转写_ASR_0918/`**：

| 文件 | 是什么 |
|---|---|
| `passA_fw-small.tsv` / `passB_wcpp-turbo.tsv` | 两路原始结果（`file / card_id / name / faction / ev / text / … / gt_from_filename`） |
| **`compare.tsv`** | 🔴 **主检视文件**：`textA ‖ textB ‖ 文件名原文 ‖ werA ‖ werB ‖ agree ‖ non_speech ‖ needs_review` |
| `wer_report.txt` | 语料级 WER/CER + 最差的 20 条 |
| **`voice_lines.asr.json`** | **sidecar，不覆盖 `voice_lines.json`** |

🔴 **`voice_lines.json` 里的 `text` 优先级**：**文件名自带原文 > 两路一致 > 单路 > 人工**。
sidecar 里每条带 `source` 字段（`filename` / `asr-both-agree` / `asr-A-only` / `asr-B-only` / `non-speech` / `empty`），
**只有前两种能自动采信**。
⚠️ **例外：判为「非人声」的一律留空**（见下）。

🔴 **泰伦那 33 条虫叫的 `text` 留空，描述不当文本**（**2026-09-18 已定案**，见 §1.6.1）：
原版**气泡文字来自 I2 本地化词条**，而这类单位语音**没有词条 ⇒ 原版就是只播音不显字**
（单位出场那条路甚至因为 `IsNullOrEmpty` 门**连气泡都不出现**）。
⇒ 文件名那段描述（`Primal roar`）**只进 `compare.tsv` 给人看**，写进 `voice_lines.json` 时 `text` 留空、`source = non-speech`。
**这条已在 `工具/asr/compare.py` 里落实**（判为非人声就清空）。

#### 🔴 2.5.1 表补全之后**必须补一轮增量转写**（2026-09-18 **已跑完**）

两路 ASR 最初是**对着旧的 1787 条**跑的，而 §1.5.1 f) 把表补到了 **1844 条** ⇒ 一度有 **57 条没覆盖**。

**怎么补的（不用改脚本）**：`runA.py` / `runB.py` 本来就**带断点续跑**
（先读同名 tsv、跳过已转过的文件，再 `append`）⇒ **不带参数重跑一遍**即可，只会补缺的那些。
实测：A 路 57 条用 **1.9 分钟**、0 错误；B 路约 7 分钟。补完两路都到 **1844 行（+表头）**。

#### ✅ 2.5.2 最终数字（2026-09-18 收尾，**全量 1844 条零缺口**）

```
音频 1844 条 · A 输出 1844 条 · B 输出 1844 条
文件名自带原文 487 条（剔除舞台指示 62 · prompt 回显 0 后参与算分 425 条）
A 路：WER 0.1627（425/2612 词） · CER 0.0770（871/11313 字符）
A/B 一致 1170 · 不一致 587 · 只有 A 5 · 只有 B 73
需人工复核 624 条（写进 compare.tsv 的 needs_review 列）
```

**sidecar `voice_lines.asr.json` 的 `source` 分布**（**决定 `text` 能不能自动采信**）：

| source | 条数 | 采信 |
|---|---|---|
| `asr-both-agree`（两路独立写出同一条） | **878** | ✅ 自动 |
| `filename`（文件名自带原文） | **425** | ✅ 自动 |
| `asr-disagree-A` | 431 | ❌ 人工 |
| `non-speech`（泰伦虫叫 + 舞台指示） | 62 | — **原版本来就不显字**（§1.6.1），`text` 留空 |
| `asr-B-only` / `asr-A-only` / `empty` | 41 / 2 / 5 | ❌ 人工 |
| **未转写** | **0** | ✅ 缺口已闭 |

⇒ **可自动采信 1303 条**，其余 **624 条**进人工队列。

⚠️ **尺子上的一个缺口（已知，还没补）**：最差 20 条里**几乎全是拟声词**
（`woosh` / `Fleeeessssshhhhh` / `rustle` / `woooshh` / `Intro`）——
而 `wer.py` 的非人声判据 `NON_SPEECH_DESC_FACTIONS = {'tyranid'}` **只管泰伦一族**。
§1.6 其实已经量过「全库 481 条带描述里 49 条是音效」⇒ **泰伦之外还有约 16 条**
没被这条规则覆盖，它们以「人声」的身份进了算分集。
⚠️ **但别急着改规则**：改了 WER 就跟着变 —— 那是「**尺子自己会坏**」那一类，
要先想清楚「这 16 条本来就该不该在算分集里」，再一次性改。
