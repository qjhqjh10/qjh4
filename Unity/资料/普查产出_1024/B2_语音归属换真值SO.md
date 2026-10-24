# B2 · 语音归属换真值 SO（EC57 孤儿的根因根治 + UM47 错标签）

> 只读 `d:/2`（未改存档）、未动 git、未改任何 `.cs` / 正本。
> 工具：Python 3.14（`python -I -X utf8`）+ UnityPy 1.24.2（`d:/2/Warpforge_tools/py312/python.exe -I`，只用于**独立复核** bundle）。
> 本轮改动：`Unity/工具/import_original_audio.py`（生成器）· `Unity/MyGame/Assets/CardPresentation/Resources/voice_lines.json`（数据）· 本报告目录三份产物。

---

## 〇、一句话结论

**根因成立并已根治**：语音归属的 SO 真值表原来是 **2022 时代 1,195 张**的快照，现换成**当前 build 的 1,351 张**。
- `EC57 Xarahan` 的 **11/13** 条 chat 语音已按原版字段落位（`gen4`/`gen5` **原版 SO 里没有任何字段引用** ⇒ 照规矩「原版空」不猜）；
- `UM47 Uriel Ventris` 那三条**不是「SO 真假不明」** —— 是**我们自己的标签错**：旧口径按 **clip 级**建真值表，把**教程版 Ventris** 的字段算到了 UM47 头上，导致 `gen4.ogg`→`gen2`、`gen7.ogg`→`gen3`。已改成**卡级**，现在 `gen4`/`gen7` 归位。
- `voice_lines.json`：**642 张卡 / 1,876 → 1,894 条**（+18）· `src=cardSO` **1,283 → 1,370** · 带 `so` 字段的行 **1,314**。

---

## 一、新 SO 源在哪 / 怎么验的

### 1·1 源

| 项 | 值 |
|---|---|
| SO 真值文件 | `D:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json`（67,051,793 B；形状 `{bundle,count,cards:[{…,full_raw}]}`，**卡 SO 全字段在 `full_raw`**） |
| 原始 bundle | `D:/2/关服前存档/新建文件夹/Everguild_Warpforge/54bdb7b1114a2de41519cac560a4bce4/ea8bca4f5c6909d3124fd4975dc96173/__data`（531,962 B · UnityFS 6000.2.6f2） |
| 旧源（已退场） | `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`（38,579,923 B · 平的 `list` · 1,195 条） |

⚠️ 这个路径**不是新造的**：`工具/gen_card_vfx_by_card.py:154` 早在 2026-10-18（`T14`）就已换到这个源，
`工具/gen_cards_truth.py:62` / `工具/gen_card_chat_intro.py`（`source.cardSO`）也都指向它 —— 本轮只是让**语音归属**这一路跟上来。

### 1·2 独立复核（不信任现成产物，直接读 bundle）

`d:/2/Warpforge_tools/py312/python.exe -I` + `UnityPy.load(<__data>)`：

```text
objects 1360 · MonoBehaviour 1359
with uniqueId 1358 · dup uid 0
has cardAbilities: True 1351 / False 7      ← 1,351 张 RawCardScript + 7 张 AlternateArtCard
```

EC57 / UM47 的 **15 个音频字段（3 战斗 + 12 chat）逐个 GUID 与 `cards_raw.json` 的 `full_raw` 对照：mismatch = 0**。
复核脚本：`D:/4/Unity/_tmp_view/b2_verify_bundle.py`（临时目录，已 gitignore）。

### 1·3 `uniqueId → SO 资产` 映射（本轮产物）

`资料/普查产出_1024/B2_uniqueId_SO_音频映射.tsv` —— **1,525 行**，列：
`uniqueId · ourCardId · inPool · cardName · field · ourEvent · guid · clip · resolveSource`
覆盖 1,351 张 SO 里**所有非空音频字段的引用**；**解不出 3 行 / 2 个 GUID**（见 §七·2）。

`uniqueId → 我们卡 id` 走 `card_vfx_by_card.json._assetRefs[].origCardId`：**1,127 对，覆盖 `cards_engine.json` 全部 1,127 张，缺 0**。

---

## 二、新旧逐卡差异汇总

逐条明细 → `资料/普查产出_1024/B2_新旧SO逐卡差异.tsv`。

| 维度 | 数 | 说明 |
|---|---|---|
| 旧表卡数 | 1,195 | 其中 **1 条 `uniqueId` 为 null** ⇒ 实际可按 uid 比的是 1,194 |
| 新表卡数 | 1,351 | `uniqueId` 1,351/1,351 非空、0 重复 |
| 共有 uid | 1,192 | |
| **新增（新独有）** | **159** | 其中**带 chat 字段的 6 张**：`DA75 Master Zacharial` · `SW1 Logan Grimnar` · `SW2 Njal Stormcaller` · `SW3 Ragnar Blackmane` · `UM87 Roboute Guilliman` · `UM88 Valius Paxor`（后 4 张是 2025-07 之后的新督军） |
| **旧独有** | **2** | `#HB_BL34` / `#HB_DA1`（异画占位对象，新包把异画挪进 `AlternateArtCard`，不再是 `RawCardScript`）|
| **共有卡里的字段差异** | **14 条 / 仅 1 张卡** | **全部集中在 `EC57`**：12 个 `*ChatSound` + `attackAudio` + `deathAudio`，**旧全空 → 新全有** |

> 🔑 **这条要记牢**：所谓「1,195 旧 vs 1,351 新」的差别**不是字段漂移**，而是 ①**EC57 那张卡的字段在旧快照里根本没解出来**（14 个空），②**多了 159 张卡**（含 6 张带 chat 的督军）。
> 共有 1,192 张里，**其余 1,191 张的 15 个字段逐条完全相同**（`same`）。

### 2·1 新源的字段覆盖（15 个字段）

| 字段 | 非空 | GUID/音频源解不出 |
|---|---:|---:|
| `summonAudio` | 602 | 1 |
| `attackAudio` | 66 | 0 |
| `deathAudio` | 68 | 2（同 1 个 GUID） |
| 12 个 `*ChatSound` | 66 / 64–66 | **0** |

⇒ **chat 一族在新源里 100% 可解**（旧快照 EC57 的 12 个全空，是唯一断点）。

---

## 三、`voice_lines.json` diff 归因表

明细 → `资料/普查产出_1024/B2_voice_lines_归因diff.tsv`（162 行）。
总账：**前 642 卡 / 1,876 条 → 后 642 卡 / 1,894 条**；**新增 21 行 · 删除 3 行 · 同键仅补 `so` 138 行**；**只涉及 8 张卡**。
> 「改前」快照 = 本轮开工时工作区里的那一版（A4 重生成后的 1,876 条）。我留在
> `D:/4/Unity/_tmp_view/b2_before.json`（**gitignore 的临时目录**，不在仓库里）——
> 该目录若被清掉，**上面那份 TSV 就是逐行留痕**，重跑不需要它。

| 卡 | 变化 | 原因 |
|---|---|---|
| **`EC57` Xarahan, Daemon Prince** | **+12 行**（`cantdo/concede/gen1/gen2/gen3/greet/hurry/intro/mirror/threat/wp` + `default`） | 旧快照 12 个 chat 字段全空 ⇒ 新快照有值 ⇒ 归属①·B 落位。`default` 是 `defaultChatSound`（= `attack` 那条 clip，原版事实） |
| **`UM47` Uriel Ventris** | **+2**（`gen4`/`gen7` 恢复）· **−2**（错标的 `gen2`/`gen3` 消失）· 3 行补 `so`（`intro`/`gen1`/`gen3`） | **口径从 clip 级改卡级**：旧口径把教程版 Ventris 的 `generic2`/`generic3` 算到 UM47 头上 |
| **`UM88` Valius Paxor** | **+2**（`default` + `gen3←_gen4.wav`）· **−1**（旧的 `gen4` 行） | **原版真值**：UM88 的 `generic3ChatSound` 指的就是 `VO_UM_Valius Paxor_gen4.wav`（文件名与字段序号不一致，原版如此）|
| `DA75` `SW1` `SW2` `SW3` `UM87` | 各 **+1 `default`**，**12 行 src: 无 → `cardSO` + `so`** | 这 5 张原来靠 ② `card_index` 兜底（归属本来就对），新快照证实是 SO 真值 ⇒ 升级为权威级（**事实纠正，不是行为变化**） |
| 其余 62 张（`SW4–SW38` · `UM89–UM108` · `DA65–DA71` · `Tyrnak and Fenrir`） | 各 1 行补 `so=summonAudio` | `deploy` 行本来就有，旧口径因 clip 级歧义没敢标字段名；卡级后能标了 |

**补 `so` 的字段分布**：`summonAudio` 63 · `deathAudio` 7 · 12 个 chat 各 6–7。
**event 变化**：本轮由 ①·B 直接定 `ev`，所以「事件名回真值」那一步的 `ev_fixed` 现读 **0**（不是失效 —— 校正在 ①·B 里就完成了，例：UM88 的 `_gen4` 现在直接是 `gen3`）。

---

## 四、`EC57` 13 条 chat 语音逐条结论

音频实体都在 `Resources/Art/audio/vo/`（有 `.meta`）。表内结果：

| # | 文件 | 结果 | `so`（原版字段） |
|---|---|---|---|
| 1 | `VO_CSM_EC_Daemon Prince Xarahan_cantDo.ogg` | ✅ 落位 `ev=cantdo` | `iCantDoThatChatSound` |
| 2 | `..._concede.ogg` | ✅ `concede` | `concedeChatSound` |
| 3 | `..._gen1.ogg` | ✅ `gen1` | `generic1ChatSound` |
| 4 | `..._gen2.ogg` | ✅ `gen2` | `generic2ChatSound` |
| 5 | `..._gen3.ogg` | ✅ `gen3` | `generic3ChatSound` |
| 6 | `..._gen4.ogg` | ⛔ **不在表里** | **原版空**（见下） |
| 7 | `..._gen5.ogg` | ⛔ **不在表里** | **原版空** |
| 8 | `..._greet.ogg` | ✅ `greet` | `greetChatSound` |
| 9 | `..._hurry.ogg` | ✅ `hurry` | `hurryChatSound` |
| 10 | `..._intro.ogg` | ✅ `intro` | `introChatSound` |
| 11 | `..._mirror.ogg` | ✅ `mirror` | `mirrorChatSound` |
| 12 | `..._threat.ogg` | ✅ `threat` | `threatChatSound` |
| 13 | `..._wp.ogg` | ✅ `wp` | `wellPlayedChatSound` |

外加（同一张卡、非这 13 条）：`attack` · `death`（`attackAudio`/`deathAudio`）· `deploy`（`summonAudio`，仍指向
`VO_UM_Primaris Intercessor - For the Imperium` —— **原版自己抄错，调度台已裁定照原版播**，本轮未动）· `default`（`defaultChatSound` = `attack` 那条 clip）。

**关于 `gen4`/`gen5`（原版空，⛔ 没猜）**：
- 1,351 张 SO 全池里，**没有任何卡的任何音频字段**指向 `Xarahan_gen4/gen5`（逐 GUID 扫过：命中 0）；
- `RawCardScript__GetChatSound.c` 的 `switch` 只有 **0..10**（`ChatMessage` 枚举也正好 11 个值，`d:/2/Warpforge_code/Scripts/Assembly-CSharp/ChatMessage.cs:1-14`）——**generic 只有 3 个入口**，没有第 4/5 个；
- 文件名词序是 `Daemon Prince Xarahan`（与卡名 `Xarahan, Daemon Prince` 相反）⇒ ③ 按名字也匹配不上（这本来就不该成为判据）。

⇒ **结论：这两条是原版的未使用资产，不是我们漏接。** 按「先 SO 真值、再 index、最后才按名字猜」的三级口径，它们停在「仍未并上的 20 条孤儿」里，**并已逐条打印**。

---

## 五、`UM47` 结论：**是我们的标签错，不是 SO 真假不明**

### 5·1 SO 真值（新源，逐字段）

| 字段 | clip |
|---|---|
| `introChatSound` | `VO_Ultramarines_Ventris_intro` |
| `mirrorChatSound` | `..._mirror` |
| `generic1ChatSound` | `..._gen1` |
| `generic2ChatSound` | `..._gen2` |
| `generic3ChatSound` | `..._gen3` |
| `defaultChatSound` / `attackAudio` | 同一个 GUID → `..._attack` |
| `concede/hurry/iCantDoThat/greet/threat/wellPlayed` | `..._concede/_hurry/_cantDo/_greet/_threat/_wp` |

**UM47 自己的 12 个 chat 字段一一对应、无冲突。**

### 5·2 旧标签错在哪

旧口径的真值表键是 **clip**：`truth[clip] = {引用过这个 clip 的所有字段}`，**不区分是哪张卡**。
而 `VO_Ultramarines_Ventris_{intro,gen1,gen3}` 同时被**教程版 Ventris**（`ec0d25faeb266ea4eaec41ae72b8d8ad`，
`cardName = Uriel Ventris (tutorial)`，不在卡池）引用：

```text
VO_Ultramarines_Ventris_intro  ← UM47.introChatSound   + tutorial.mirrorChatSound
VO_Ultramarines_Ventris_gen1   ← UM47.generic1ChatSound + tutorial.introChatSound (还有第 3 个重复 SO b5eedae3…)
VO_Ultramarines_Ventris_gen3   ← UM47.generic3ChatSound + tutorial.generic1ChatSound
VO_Ultramarines_Ventris_gen4   ← **只有** tutorial.generic2ChatSound
VO_Ultramarines_Ventris_gen7   ← **只有** tutorial.generic3ChatSound
```

⇒ ①·B 判「两个 chat 字段共引 ⇒ 不认」，`intro/gen1/gen3` 掉到 ②/③（无 `so`）；
⇒ `apply_event_truth` 拿 clip 级真值把 **`gen4`→`gen2`、`gen7`→`gen3`**（张冠李戴）。

### 5·3 改法与结果

真值表键改成 **`(我们卡 id, clip)`**：只看**这张卡自己**的字段。

| 行 | 改前 | 改后 |
|---|---|---|
| `_intro` | `ev=intro`，无 `so` | `ev=intro`，`so=introChatSound` |
| `_gen1` | `ev=gen1`，无 `so` | `ev=gen1`，`so=generic1ChatSound` |
| `_gen3` | `ev=gen3`，无 `so` | `ev=gen3`，`so=generic3ChatSound` |
| `_gen4` | **`ev=gen2`（错）** | `ev=gen4`（无 `so` —— UM47 的 SO 里没有这个字段） |
| `_gen7` | **`ev=gen3`（错）** | `ev=gen7`（无 `so`） |

⚠️ `gen5`/`gen6` 本来就没被任何字段引用，`ev` 一直是文件名推的 `gen5`/`gen6`（不变）。

> 附带收益：`EC55 Lucius` / `EC56 Kaphrael` 等此前「EC 那族 GUID 解不出」的卡已经由 2026-10-23 的
> `guid_map_bundles.tsv`（catalog 补短键）解决，本轮**没有回归**（EC55 30 行 / EC56 18 行，chat 全带 `so`）。

---

## 六、验收怎么跑

```bash
cd d:/4/Unity

# ① 只报告不写盘 —— 期望 rc=0，且出现这几行关键读数
PYTHONIOENCODING=utf-8 python -I -X utf8 工具/import_original_audio.py --check
#   音频源：1857 个文件（1857 个不同文件名）
#   ⚠️ 同一 clip 被多张卡的卡 SO 引用（照原版都挂）：VO_UM_Primaris Intercessor - For the Imperium → ['EC57', 'UM6']
#   并进我们卡表 672 条 · 同一张卡两 chat 字段共指、不认 0 条 · 卡不在我们卡表 0 条
#   卡片：有语音 642 张 · 台词 1894 条（其中 归属① 1370 条 · 归属③按名字猜 68 条）
#   仍未并上的 20 条  ← 里面应有 Xarahan_gen4 / gen5，**不应有** 其余 11 条
echo "rc=$?"

# ② 幂等：连跑两次写盘，sha256 必须逐字节相同
PYTHONIOENCODING=utf-8 python -I -X utf8 工具/import_original_audio.py >/dev/null
sha256sum MyGame/Assets/CardPresentation/Resources/voice_lines.json
#   本轮实测：16a977df0288679059729f2fa50606ebfe75afc0e8c648eaaf4940da44d0000d
PYTHONIOENCODING=utf-8 python -I -X utf8 工具/import_original_audio.py >/dev/null
sha256sum …  # 必须同上

# ③ 三条硬断言（现读全绿）
PYTHONIOENCODING=utf-8 python -I -X utf8 -c "
import json,io
d=json.load(io.open('MyGame/Assets/CardPresentation/Resources/voice_lines.json',encoding='utf-8'))
by={c['id']:c for c in d['cards']}
ec={r['ev']:r for r in by['EC57']['lines'] if r.get('so')}
need=['intro','mirror','hurry','cantdo','concede','greet','threat','wp','gen1','gen2','gen3']
assert all(k in ec for k in need), [k for k in need if k not in ec]
assert sum(1 for r in by['EC57']['lines'] if r.get('src')=='cardSO')==15
um={r['file']:r for r in by['UM47']['lines']}
assert um['VO_Ultramarines_Ventris_gen4.ogg']['ev']=='gen4'
assert um['VO_Ultramarines_Ventris_gen7.ogg']['ev']=='gen7'
assert um['VO_Ultramarines_Ventris_intro.ogg'].get('so')=='introChatSound'
print('OK', len(d['cards']), sum(len(c['lines']) for c in d['cards']))
"
```

⚠️ **本轮没有跑 Unity**（纯数据 / 纯脚本，按铁律 12 不用跑）；运行时 `VoiceLines.cs` 一行未动。

---

## 七、没查清 / 没做

1. **`EC57` 的 `gen4`/`gen5`** —— 原版 SO 无字段引用（已按「原版空」处理，**没有按文件名猜着补**）。
   没做的部分：**没有把 1,351 张 SO 的 `customIntroChatData` / `chatAudioReferenceCard` 等旁路字段全部展开**
   去证明这两条「永不出声」（判据只做到 `GetChatSound` 的 switch 0..10 + 枚举 11 值这一层）。
2. **2 个 GUID 仍解不出**（3 条引用）：`BL47 Disgustingly Resilient` 的 `summonAudio`（`fda6e5bccaf40a14aa3b7f0249bd8dd8`）·
   `GOF2 Makari the Grot` 的 `deathAudio`（`11641bc635cf50942b52e516f66d2ba6`，另一张 uid 为裸 GUID 的卡也引它）。
   三份 GUID 表（`_assetRefs` / `guid_map_bundles.tsv` / `anim_address_map`）**都解不出**；旧
   `资料/普查产出_第二十七会话/guid_map.tsv` 我实测**也 0 命中** ⇒ 真没有，**没猜**。
3. **56 条行仍无 `so`**：同一张卡的 SO 用**两个字段**指同一条 clip（`attackAudio` + `defaultChatSound`，66 张卡全同）。
   这是原版事实，`apply_event_truth` 判不了「这一行代表哪个角色」⇒ 按「宁可认不出」不标。**行为无影响**（`ev` 不变）。
4. **同卡同 `ev` 多行 12 处**（见 §八·3）—— 本轮**没有**改这些（不在本任务范围、且需要先拍板口径）。
5. **`So` 的消费点没验**：`VoiceLines.cs` 的 DTO 不声明 `so`/`src`（`JsonUtility` 会忽略），所以本轮是**纯数据/留痕**改进，
   运行时行为只随 `ev`/`file` 改变（即 EC57 的 12 条新行、UM47/UM88 的 `ev` 纠正）。

---

## 八、顺手发现（**只报不改**）

1. 🔴 ~~**`defaultChatSound` 在这个 build 的取音链上不可达**~~ **🔴 2026-10-24 更正（铁律 5 · B4 用指令流推翻）：这条不成立 —— `defaultChatSound` 是原版真在用的兜底，我们现有 `{intro, default}` 链与原版一致，代码不该改。**
   错因：**把 Ghidra `.c` 的标签缩进读成了控制流**。B4 用 `rva_of_method.py` + `disasm_va.py` 复读 `RawCardScript$$GetChatSound`（RVA `0x946C80`）：default 支在 `0x180946DE3 call CustomDebug.LogWarning` 之后**没有 `jmp`**，下一条 `0x180946DE8` 就是**各 case 支共用的尾巴**（`.c` 里的 `goto LAB_180946de8` 被画在了 if 块体内）；尾巴 `op_Inequality(clip,0)` 为假 ⇒ `0x180946E0C je 0x180946E47` ⇒ `mov rcx,[rbx+0x188]`（= `defaultChatSound`，`dump.cs:23300`）⇒ 尾调 `AssetReferenceTyped.Load`。关键旁证：各 case 支在 `Load()` 拿不到 clip 时也是 `jmp 0x180946DE8`，**与 default 支汇到同一处**；而 `cards_raw.json` 里这些字段是 `{m_AssetGUID:"", …}`（对象在、GUID 空），`Load()` 返回空 ⇒ **正常走 `+0x188` 兜底**。
   ⇒ 56 条 `ev="default"` 行**保留**、`VoiceLines` 的 default 第二跳**不动**；判据已写进 `Core/VoiceLines.cs` 文件头，`AnimFXCheck` ⑤f 有两条**反向护栏**（断言它**取得到**）。判据全文 → `资料/普查产出_1024/B4_语音兜底两处.md`。
2. **`资料/普查产出_第二十七会话/guid_map.tsv` 对音频的覆盖率**：620 条 AudioClip，对当前缺口**补 0 条**。
   `gen_card_chat_intro.py` 把它列为 fallback（只补 primary 没有的）—— 无害，但**别指望它救场**。
3. **同卡同 `ev` 多行 12 处**（`voice_lines.json` 现状），其中 **3 处是「SO 指定的文件 ≠ 文件名同名文件」**：
   - `ASH2 Eliac Zephyrblade`：`generic1` 的 SO 指 `_gen4.ogg`，同时表里还有 `_gen1.ogg`（无 `so`）；
   - `ASH2` 的 `intro`：SO 指 `_gen5.ogg`，同时还有 `_intro.ogg`；
   - `UM88 Valius Paxor`：`generic3` 的 SO 指 `_gen4.wav`，同时还有 `_gen3.wav`。

   `VoiceLines` 在同 `ev` 多条时**随机挑一条** ⇒ 这三处可能挑到 SO 没指定的那个文件。**这是旧口径就有的**（不是本轮引入），
   要修得先定口径（「SO 优先、同名文件让位」？），**本轮没动**。
4. **`cards_raw.json` 是 gitignore 的本地大文件**（`.gitignore:243`，67 MB）。生成器现在依赖它；
   换机器/清缓存后 `import_original_audio.py` 会报「读不到卡 SO 本体」并**整级失效（出声，不静默）**。
   ⇒ 重新生成的办法见 `资料/普查产出_1018/allcards_1350/报告.md`（UnityPy 读 `54bdb7b1…/ea8bca4f…/__data`）。
   `voice_lines.json` 本身**已提交**，所以**运行时不依赖它**。
5. **`card_vfx_by_card.json` 与本轮同源**（`gen_card_vfx_by_card.py:154` 早已是 `cards_raw.json`）——
   所以 `origCardId` 映射覆盖 1,127/1,127，**不需要另外重生成**。⚠️ 该文件当前在 git 里是 `M`（别的代理改过），**我没碰**。

---

## 九、本轮改动清单

| 文件 | numstat（增/删） | 性质 |
|---|---|---|
| `Unity/工具/import_original_audio.py` | 184 / 116 | **生成器**：SO 源换当前 build（含 `full_raw` 展平）· ①·B 与 `apply_event_truth` 从 clip 级改卡级 · 换源/口径注释 · `defaultChatSound` 注释订正 |
| `Unity/MyGame/Assets/CardPresentation/Resources/voice_lines.json` | 731 / 389 | **数据**：+18 行 / −3 行 / 138 行补 `so`（只涉及 8 张卡）|
| `Unity/资料/普查产出_1024/B2_语音归属换真值SO.md` | 新 | 本报告 |
| `Unity/资料/普查产出_1024/B2_uniqueId_SO_音频映射.tsv` | 新 | `uniqueId → SO 音频字段` 映射（1,525 行）|
| `Unity/资料/普查产出_1024/B2_新旧SO逐卡差异.tsv` | 新 | 新旧快照逐卡逐字段差异（175 条：14 字段差异 + 159 新增卡 + 2 旧独有）|
| `Unity/资料/普查产出_1024/B2_voice_lines_归因diff.tsv` | 新 | 前后逐行归因（162 行）|
