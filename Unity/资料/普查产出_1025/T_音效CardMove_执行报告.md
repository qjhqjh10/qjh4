# T · 音效线：补 `CardMove` cue + 接通「卡牌换位音」（2026-10-25）

> 执行代理 `T` · 单件活 · 判据由审查代理 `AU4` 现查给出，本报告**逐条亲核过一遍**（其中一条把它订正了）。

---

## 一、结论

**做完了**：卡牌放大窗（战斗版 `Battle/CardDisplayWindow`）的**换位音**已接通，
数据侧（`animfx_sounds.json` 里的 `CardMove` cue）与调用侧（`SwapToFront()`）两边都补上，并配了独立自检入口。

| 项 | 状态 |
|---|---|
| `CardMove` cue 进表 | ✅ 461 条（原 460 +1），4 个 clip，参数 = 资产实读 |
| 换位入口请求它 | ✅ `CardDisplayWindow.SwapToFront()` 在 `_swapping = true;` 之后、`ArtOverdraw` 之前喊 `WindowSounds.PlayFx(SwapSoundCue, …)` |
| 生成器幂等 | ✅ 连跑 3 次 md5 **逐字节一致**（`c4ff81d8ebdb1abb33923ce345acd76d`） |
| 断言 | ✅ 新增 `Editor/SoundCueAssertT.cs`（10 条硬断 + 1 条元断言 + 1 条软检查）**并接到 `CardFaceAssertE5.Run()` 末尾** ⇒ 跑 `-executeMethod CardFaceAssertE5.Run` 就会执行（见 §五·b） |
| 类型检查 | ✅ `TMPDIR=/tmp/wf_t1 bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**（接宿主后复跑仍是 0/0） |
| ⛔ 未做 | 没跑 Unity（红线）⇒ 断言**尚未在 Unity 里执行过**；真机听感未验 |

---

## 二、判据（逐条亲核，出处给到字段）

### 2·1 引用者：**14 个 MB 全指同一个 pid**
- 亲扫 `d:/2/新解包资源/assets_full/bundle_*/MonoBehaviour/*.json`：带 `soundOnCardSwap` 字段的 MB **共 14 个**
  = **13 个战场**（`bundle_scenes_scenes_battlearena1` … `…ta uviorla`）+ **主菜单**（`bundle_scenes_scenes_mainmenuwarpforge`）。
- **14 个全是同一个值**：`{"m_FileID": 12, "m_PathID": 4812904303840730115}`（脚本核对：`distinct pids {4812904303840730115}`）。
  例：`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4750.json:110-111` ·
  `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_2033.json`。
- ⇒ 这不是「某一场景的特例」，是**这个窗口类的一个字段**。

### 2·2 cue 资产 = `CardMove`（4 clip）
`d:/2/新解包资源/assets_full/bundle_soundcollection_assets_all/MonoBehaviour/CardMove_4812904303840730115.json` 实读：

| 字段 | 值 |
|---|---|
| `m_Name` | `CardMove` |
| `clipList` | 4 条 PathID：`-6740773956303463874` / `5303275677218709805` / `-1522524771764259241` / `8024429088123589697` |
| `minPitch` / `maxPitch` | **0.8 / 1.1** |
| `minVolume` / `maxVolume` | 1 / 1 |
| `timeToPlayAgain` | 0.1 |

**🔴 铁律 5 就地订正 `AU4` 报告**：`资料/普查产出_1025/AU4_场上卡牌模型与展示_审查与修复.md` §三·1 写
「`minPitch=maxPitch=1`」——**资产实读是 0.8–1.1**（与 `CardStartDrag` 同值）。以资产为准；本轮落表用的是 0.8–1.1。

**三条近名资产必须分清（⛔ 别混）**：
| 资产 | 是什么 | 与本次的关系 |
|---|---|---|
| `MonoBehaviour/CardMove_4812904303840730115.json` | cue `CardMove` · **4 clip** | ✅ 本笔要的就是它 |
| `MonoBehaviour/CardStartDrag.json` | cue `CardStartDrag` · **1 clip** = `CardMove`（**那 4 条里的第 1 条**） | 是**另一个 cue**；它证明了「表里有 clip 名 `CardMove`」≠「有 cue `CardMove`」 |
| `MonoBehaviour/Cardmove.json`（小写 m） | cue `Cardmove` · 1 clip · pitch 1.0 | **另一个资产**，本笔不要（自检 T-5 钉着） |
| `Resources/battle_sounds.json` 的 `moveCardIntoBattle` / `createCardInHand` | **战斗「原版字段名」表**，`clips` 里出现 `CardMove` / `CardMove_1..3` 是 **clip 名** | 键是字段名 ⇒ 与 cue 名 `CardMove` **撞不上**（`WFSoundBank` 撞名检查按 `name`，见 `WFSoundBank.cs:118-125`） |

### 2·3 消费点（位置也是判据）
`d:/2/tools/decomp_full/CardDisplayWindow__ChangeCardPosition.c:68-73`（亲读）：

```c
SoundManager__Play2D(&local_38, ***(DAT_18429e748 + 0xb8), *(undefined8 *)(param_1 + 0xd8), 0, 0);
```

- 第 4 实参 `0` = **MixerType.FX**（`MixerType.cs`：`FX=0 · Music=1 · Voices=2 · Jingles=3`）；
- `+0xd8` = `soundOnCardSwap`；
- **位置**：紧跟 `SetupTutorialObj` / `+0x150 = 1`，**早于** `SetImageOverDraw`（`:75-79`）与 `DOTween.Sequence`（`:84+`）。

---

## 三、改动清单（白名单内 + 一个新建自检文件）

| 文件 | 改了什么 |
|---|---|
| `工具/import_original_sfx.py` | `EXTRA_CUES` 增一条 `"CardMove"`，带 20 行判据注释（pid / 14 实例 / 消费点 / 4 clip / 三条近名资产 / `AU4` 订正） |
| `MyGame/Assets/CardPresentation/Resources/animfx_sounds.json` | **仅经生成器写**：+1 cue（`14 增 / 0 删` 行） |
| `MyGame/Assets/CardPresentation/Battle/CardDisplayWindow.cs`（CRLF，**1823/1823 未翻**） | ① 新增 `public const string SwapSoundCue = "CardMove"`（带完整判据 `<summary>`，自检的锚点）<br>② `SwapToFront()` 里插入 `WindowSounds.PlayFx(SwapSoundCue, …)`（位置 = 原版那一跳）<br>③ 换位收尾那段 `Debug.Log` 带上 cue 名<br>④ 把 `AU4` 留下的「【仍欠】… 挡在数据侧」注释**就地订正**为「已接通 + 两句证据」 |
| `MyGame/Assets/CardPresentation/Editor/SoundCueAssertT.cs`（**新建**） | 自检本体：`SoundCueAssertT.Run()`（10 条硬断 + 1 条元断言 + 1 条软检查） |
| `MyGame/Assets/CardPresentation/Editor/SoundCueAssertT.cs.meta`（新建） | 照本目录既有的 **2 行模板**（`fileFormatVersion: 2` + `guid:`，无 `MonoImporter`、无尾随换行；63 份现存 meta 全是这个格式），guid `cc125f5912a744f0950c24a8f8207f61`（与全仓 11,739 条 guid 查重无冲突） |
| `MyGame/Assets/CardPresentation/Editor/CardFaceAssertE5.cs`（CRLF，**578/578 未翻**） | 🆕 **接宿主**（协调代理追加要求 · 台账 `AUD` 那条「跑不到」）：`Run()` 末尾加 `RunSoundCueAssertT()`，把 `SoundCueAssertT` 的断言跑起来并把计数并进 `E5 合计`；顶部「判绿红」说明补上 `BT T` 前缀行的数法 |

> **为什么新建自检文件**：简报要求「补一条断言」但同时点名 `Editor/BattleScene.cs` **另有代理在改**。
> `Editor/BadgeFilterAssert.cs` 的做法是挂到 `BattleVfxAssertE3.Run()` 末尾 —— 那条链也要改别人的文件，
> 且同样有撞车风险 ⇒ 先做成**自包含**的类。
> 🔴 **2026-10-25 追加（协调代理 · 台账 `AUD` 查出的真缺陷）**：它当时**全仓零调用** ⇒ 那 10 条断言
> 等于没验。现已按协调代理的指示接到 **`CardFaceAssertE5.Run()`**（卡面/窗归属；`BattleScene.cs` 仍不碰），
> 见 §五·b。两种跑法都还在：宿主 `CardFaceAssertE5.Run` / 单独 `SoundCueAssertT.Run`。
>
> ⚠️ **越白名单说明**：本笔碰了简报白名单之外的 **3 个文件** —— `Editor/SoundCueAssertT.cs`（+meta，
> 需求 4 要求的断言没别的不撞车落点）与 `Editor/CardFaceAssertE5.cs`（协调代理**明确追加要求**接宿主）。
> **没有改动任何既有断言的期望值**，也没有动 `BattleScene.cs` / `Shell/**` / `RuleEngine/**` / 正本。

---

## 四、生成器幂等证据

```
run1：md5 c4ff81d8ebdb1abb33923ce345acd76d
run2：md5 c4ff81d8ebdb1abb33923ce345acd76d
run3（改完注释后再跑）：md5 c4ff81d8ebdb1abb33923ce345acd76d
git diff --numstat：14  0  Resources/animfx_sounds.json      ← 只多这一个 cue 块，零删除
```

生成器输出（run3 关键三行）：
```
合起来：cue 包装 612 个 · 能对上的音频文件 622 个
查漏（反向）：… ⇒ **0 个候选**          ← 新增 cue 没有带出新漏网
解析成功 461/461 个 cue；要用到 485 个音频文件
已拷 0 条 · 已存在跳过 485 条 · 补 meta 0 条    ← 4 条 clip 的音频与 .meta 本来就在
```

落表结果（现读）：
```json
{"name":"CardMove","clips":["CardMove","CardMove_1","CardMove_2","CardMove_3"],
 "minPitch":0.8,"maxPitch":1.1,"minVolume":1.0,"maxVolume":1.0,"timeToPlayAgain":0.1}
```
音频本体（`Resources/Art/audio/sfx/`，gitignore 目录，本机都在，含 `.meta`）：
`CardMove.wav 44,204 B` · `CardMove_1.wav 23,468` · `CardMove_2.wav 17,324` · `CardMove_3.wav 19,500`。

---

## 五、自检：`SoundCueAssertT.Run()`（新建入口）

| # | 断什么 | 🧨 改回旧写法会怎样 |
|---|---|---|
| T-0 | 前提：表载得到、cue 数 > 400 | 表没进仓库 ⇒ 下面全是假结论 |
| T-1 | `TryGetCue(CardDisplayWindow.SwapSoundCue)` 为真 | `EXTRA_CUES` 删掉它 ⇒ 红（真机上还会让 `BadCues++`） |
| T-2 | clip **恰好 4 条**且集合 = `{CardMove, CardMove_1, CardMove_2, CardMove_3}` | 收成 `CardStartDrag` 那条 1-clip / 拿错小写 `Cardmove` ⇒ 红 |
| T-3 | pitch 0.8–1.1 · vol 1–1 · ttp 0.1 = **资产实读** | 认错资产 ⇒ 红（挡 `AU4` 那个 pitch=1 的错口径） |
| T-4 | `SwapSoundCue == "CardMove"` | 代码侧改名迁就错 cue（或反之）⇒ 红 |
| T-5 | 表里**没有**小写 `Cardmove` | 把 `EXTRA_CUES` 写成小写 ⇒ 红 |
| T-6 | `CardStartDrag` 仍是 1-clip、clip 名 `CardMove` | 两条 cue 合并 / 起拖音被顶掉 ⇒ 红 |
| T-7 | `WindowSounds.PlayFx(SwapSoundCue)` 真的走通（`Enabled=false` 下返回 true、`Played==1`） | cue 缺失 / 改名 ⇒ 返回 false ⇒ 红 |
| T-8a | **读源码**：`SwapToFront` 里 `WindowSounds.PlayFx(SwapSoundCue` **恰好一处**（非注释行） | 删掉（换位变默片）⇒ 0 处 ⇒ 红；写死别的 cue ⇒ 不再引用常量 ⇒ 红 |
| T-8b | **读源码的顺序**：`_swapping = true;`（现 1580）< PlayFx（现 1591）< `b.ArtOverdraw = true;`（现 1603） | 挪进 `OnComplete` / 挪到补间之后 ⇒ 顺序不成立 ⇒ 红 |
| T-8（软） | 4 条 clip 的音频文件加载得到 | `Resources/Art/` 是 gitignore ⇒ **只警告不红**（同 `Editor/BattleScene.cs:14373-14374` 的既有口径） |
| T-9（元） | **扫 `Assets/**/*.cs`** 断 `SoundCueAssertT.Run()` **至少有一个外部宿主**在调（排除本文件自己） | 把宿主那条调用删掉 ⇒ 0 处 ⇒ 红（正是本轮台账那条缺陷）。⚠️ **自指局限**：若唯一宿主被删，本断言自己也跑不到 —— 非自指保障 = `CardFaceAssertE5.Run` 在批跑清单里（卡牌线总表 `:16` 收口读数 / 界面线总表 `:62` 待跑） |

> **为什么 T-8 读源码**：纯数据断言证明不了「调用点还在」（有人把那一行删掉，表照样全绿）。
> 本仓先例：`Editor/BattleScene.cs:1655` / `:6427` / `:6754` / `:12177` 都读 `.cs` 行来断。
> 我在**离线**用同一套判据跑过一遍：`swapping_true=1580 < playfx=1591 < overdraw=1603`、`n_play=1` ⇒ 这两条会绿。
> **⚠️ 但它们没有在 Unity 里真跑过**（红线：不跑 Unity）。

**怎么跑**（主对话 · Unity 批处理全局串行）：
```bash
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod CardFaceAssertE5.Run -logFile -
# ↑ 推荐：一趟把菜单侧（D 组）+ 战斗侧（T 组）都验了
# 判绿红：`grep "^E5"` 的合计行（已含 T 组）· 明细 = `grep -E "^(E5|BT T) [✓✗]"`

# 只想验战斗侧这一件：
#   … -executeMethod SoundCueAssertT.Run -logFile -
#   （判绿红：`BT T === 断言合计：N 通过 / M 失败 ===`）
```

---

## 五·b、宿主接线（2026-10-25 追加 · 协调代理 / 台账 `AUD`）

**缺陷**：`SoundCueAssertT.Run()` 当时**全仓零调用**（`grep -rn "SoundCueAssertT"` 只命中它自己 + 注释）
⇒ 上面 12 条断言**永远跑不到** = 没验。台账原文：`资料/任务线/音效线/任务总表.md:20`。

**修法**（一处，按协调代理给的归属建议）：接进 `Editor/CardFaceAssertE5.Run()`
（换位音是**卡面/窗**的事，E5 已有 D 组 = 菜单侧同一个 cue；且它**已经是**一个在跑的入口 ——
卡牌线总表 `:16` 的收口读数 `CardFaceAssertE5.Run 50/0`、界面线总表 `:62` 记的「待主对话跑」）。
`Editor/BattleScene.cs` 仍然不碰。

```csharp
// CardFaceAssertE5.Run() 末尾（RunSwapSound(pool); 之后）
RunSoundCueAssertT();          // → SoundCueAssertT.Run(); _pass += …Passed; _fail += …Failed; WindowSounds.ResetCounters();
```

**计数隔离（协调代理点名的风险）** —— 三条一起做：
1. `SoundCueAssertT` 的 T-7 **自己**在 `PlayFx` 前后各 `WindowSounds.ResetCounters()` 一次，只读
   **「单次增量 == 1」**（不看历史累计）⇒ 不会与 E5 D 组那几个 `Played` 读数互相污染；
2. 宿主在调用后再 reset 一次兜底（`RunSoundCueAssertT()` 最后一行）；
3. **调用点排在 `RunSwapSound(pool)` 之后** —— D1 的负例（「只开窗不换位 ⇒ Played == 0」）读的是它自己
   reset 之后的相对增量，排在它后面不会改写它的语义。
计数**合并**：`Passed`/`Failed` 用 `+=` 并进 `E5 合计`（与 `BattleVfxAssertE3` 挂 `BadgeFilterAssert` 同一个先例）。

**元断言**（T-9）：扫 `Assets/**/*.cs` 的非注释行找 `SoundCueAssertT.Run()`，**排除本文件自己**；
离线按同一判据跑过：现读 **1 处**（`CardFaceAssertE5.cs:530`）⇒ 会绿。
⚠️ 如实说明它的**自指局限**写在断言文案与代码注释里：唯一宿主被删时它自己也跑不到。

**已验证**：接完后 `TMPDIR=/tmp/wf_t1 bash 工具/typecheck.sh` → 运行时 **0** / 编辑器 **0**；
`CardFaceAssertE5.cs` 行尾 **578 CRLF / 578 LF（未翻）**。
**未验证**：没有跑 Unity（红线）⇒ `CardFaceAssertE5.Run` 这一趟**尚未执行**，等主对话在批跑里跑。

---

## 六、没查清 / 仍欠（**没做的也在这一节说清**）

1. ✅ **菜单版同一个缺口 —— 已由另一个代理（执行代理 `D`）补上**（本笔只记不动，§六 原文作订正）：
   `Shell/CardDetailPopup.cs` 的 `SwapToFront()` 现在有 `WindowSounds.PlayFx(CardDisplayWindow.SwapSoundCue, …)`
   （`:1064-1073`，位置 = `_swapping = true;` 之后、`b.ArtOverdraw = true;` 之前，与战斗版逐字同形），
   并有 E5 **D 组**钉着（D1 负例「只开窗不换位 ⇒ 一声不出」· D3 正例「换位 ⇒ 恰好 1 次」·
   D4a/D4b 读源码断「恰好一处 + 位置」）。⇒ 原「建议另派 Shell 白名单代理」**不再需要**。
   （判据同源：MB_2033 与 MB_4750 的 `soundOnCardSwap` 是同一个 pid。）
2. **真机听感未验**：本机无法在自检里确认「响的是不是那一声」。建议把
   `资料/真Play待验清单.md` 的 **R5**（`AU4` 报告 §五 R5：「放大窗里点后面那张卡换位，听是否有一条短促的 move 音」）
   与本笔一起批量验收（一次开游戏验多条线的口径）。
3. **`AU4` 报告 §三·1 的 pitch 数值已订正**（0.8–1.1，不是 1）——只写在本报告与两处代码注释里；
   `资料/普查产出_1025/AU4_…md` 本身**没改**（它不在本笔白名单，也不是正本；请主对话决定要不要落进 AU4 那份）。
4. **`CardMove` 是否还有别的消费点**：除 14 个 `soundOnCardSwap` 外，本笔没有再全库找「谁还引这个 pid」
   （`grep` 只按字段名找的）。若将来发现别的窗口也引它，本笔的 cue 已可复用，不需要再动数据。
5. **行尾**：`CardDisplayWindow.cs` 改完仍是 **1823 CRLF / 1823 LF**（未翻）；`import_original_sfx.py` /
   `animfx_sounds.json` 本来就是 LF，保持 LF；`Editor/CardFaceAssertE5.cs` 改完仍是 **578 CRLF / 578 LF（未翻）**；新建的两个文件是 LF。

---

## 七、出处速查

- 字段：`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4750.json:110-111`（14 个实例全同值）
- cue：`bundle_soundcollection_assets_all/MonoBehaviour/CardMove_4812904303840730115.json`
- 消费点：`d:/2/tools/decomp_full/CardDisplayWindow__ChangeCardPosition.c:68-73`（FX = 0，在补间之前）
- 数据产出：`工具/import_original_sfx.py`（`EXTRA_CUES`，2026-10-25 那条）
- 落地：`MyGame/Assets/CardPresentation/Battle/CardDisplayWindow.cs`（`SwapSoundCue` · `SwapToFront`）
- 自检：`MyGame/Assets/CardPresentation/Editor/SoundCueAssertT.cs`
- 既有口径：`MyGame/Assets/WarpforgeVFX/Runtime/WFSoundBank.cs`（撞名不静默覆盖 · `BadCues`）·
  `MyGame/Assets/CardPresentation/Shell/WindowSounds.cs`（`PlayFx` = FX · `timeToPlayAgain` 节流 · 查不到只报一次）
