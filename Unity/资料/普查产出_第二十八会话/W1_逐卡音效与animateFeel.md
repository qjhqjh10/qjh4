# W1 —— A1817 逐卡音效精确绑定 + A1818 `animateFeel` 存盘断言（第二十八会话 · 写手报告）

> 判据顺序 = ① 原版反编译/原版资产字段 → ② 解包资源 → ③ 我们的代码。代码位置相对 `d:/4/Unity/MyGame/Assets/`。
> 只动了白名单里的 5 个文件（+ 自己新建的本文件）。**没跑 Unity 批处理**；`.cs` 每次改完都跑了秒级类型检查（末次 **0/0**）。

## A1817 逐卡音效精确绑定

### 改了什么（逐文件 · 逐处 · 带行号）
1. **`工具/gen_card_vfx_by_card.py`**
   - `:176-183` 新增 `AUDIO_SO_FIELD_EVENT` = `{summonAudio→deploy, attackAudio→attack, deathAudio→death}`（**翻译表只此一处**，附反向表供打印）。
   - `:1273-1333` 运行时那份的写盘循环里，从 `byCard[*]._assetRefs`（`assetKind=="audio"`）取 `assetName`，按 `{"ev","n"}` 写进 `cards[].audio`；**同一 (卡,事件) 只取第一条**；解不出名字的**不写但计数**。
   - `:1336-1352` 生成后拿 `Resources/Art/audio/vo/` 的**实际文件**对账，缺的逐条进 `PROBLEMS`（**出声**）；目录不在则如实打印「没查」。
   - `:1355-1387` `rt` 增加 `audioCount` / `audioCardCount` / `audioNoNameCount` + `_note` 说明；**空的那一档不写这个键**（写 `[]` 会让 `VfxMap` 给这 10 张只有语音的卡建空字典条目 ⇒ `CardTableCards` 从 979 变 989 的无声副作用）。
2. **`CardPresentation/Resources/card_vfx_by_card.json`**（生成物，**重跑生成器**产出）：989 张卡（原 979 + 10 张**只有语音**的），VFX 部分**逐条不变**（已 diff 过）。
3. **`CardPresentation/Core/VoiceLines.cs`**（`:89-255`，新增 169 行、0 删除）
   - `ExactDeploy/ExactAttack/ExactDeath` + `ExactEvents[]`（`:116-122`）；DTO `RtAudio/RtCard/RtFile`（`:127-129`）。
   - `EnsureExactLoaded()`（`:154-224`）懒加载 —— **资源路径借用 `VfxMap.CardTableResourcePath`**（⛔ 不另写字符串）；载不到/解析失败**都 `Debug.LogWarning/Error`** 并留 `ExactError`；顺带建 `_textByFile`（音频文件名→台词原文，给气泡用）。
   - `TryPickExact(cardId, evt, out clip, out text)`（`:230-242`）、`SuffixOrderFor(evt)`（`:246-255`）、`ExactReady/ExactCards/ExactEntries/ExactCardIds`。
4. **`CardPresentation/Battle/BattleDriver.cs`** `SpeakFor`（`:3935-3986`）：`switch (e.Kind)` 改成产**事件名**（`VoiceLines.ExactDeploy/…`）而不是直接给后缀数组；取值改成
   `①TryPickExact → Clip` **取到就用**（`:3968-3971`）→ `②clip==null 才退 SuffixOrderFor + TryPick`（`:3973-3978`，**老行为一字未改**）。
5. **`CardPresentation/Editor/BattleScene.cs`**：见下「断言」。

### 判据（原版值 vs 我们的值，一处一行）
- 原版卡 SO（`RawCardScript`，签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/RawCardScript.cs:54/56/58`，类型 `AssetReferenceTyped<AudioClip>`）：`summonAudio` **505** / `attackAudio` **41** / `deathAudio` **42** = 588 条。
- 我们写进运行时的：**586 条 / 545 张卡**（`deploy` 504 · `attack` 41 · `death` 41）。差的 2 条 = `assetName` 为 null（`GOF2` death / `BL47` summon，GUID 在 `guid_map.tsv` 里就没有）—— **计数出声、不猜**。
- 我们**现有**（后缀猜的那套）vs 原版精确绑定（用同一套顺序表离线算的）：**537 条本来就同一条** · **12 条换成另一条** · **33 条兜底结构上给不出**（那张卡不在 `voice_lines.json` 里）⇒ 接之前**一句都不说**。
- 🔴 **4 条对不上（就是「哪几条对不上」的答案）**：卡 SO 引用了、但 `Resources/Art/audio/vo/` 里**没有**这几个文件 ——
  `VO_CSM_EC_KonstrictusTormentor (Andrew)`（EC17）· `VO_Goff_Trukkboy - Out of da trukk ready ta chop`（GOF5）·
  `VO_Tyranid_Leviathan_ScreamerKiller - Savage scream`（TL21）· `VO_UM_Invader ATV - We will carve a path through enemy lines`（UM57）。
  **这 4 条在 `d:/2/新解包资源/assets_full/bundle_*cardassets_assets_all/AudioClip/` 里都在**，是 `工具/import_original_audio.py` **只按 `card_index.json` 的引用导**、而这 4 张卡的引用不在 index 里 ⇒ **没被拷进来**（⇒ 顺手发现①）。
- 事件 ←→ 字段的对应：反编译**方法体**里 grep `summonAudio|attackAudio|deathAudio` = **0 命中**（IL2CPP 走字段偏移，名字不在体里）。判据取两条：① 字段名 + 类型（上面签名桩）；② 与我们的台词表**逐条交叉核对**：`summonAudio` **472/505** 落在我们标 `ev=line`（出场台词）· `deathAudio` **38** 标 `death` · `attackAudio` **37** 标 `attack` ⇒ 语义自洽。

### 断言（新增哪几条、各验什么、灭自证是哪条）
`Editor/BattleScene.cs` 的 `14c. 单位语音条` 块里（`:10954-11022`）：
1. `:10959` `ExactReady` + 卡数/条数（+`ExactError`）。
2. `:10980` ★ **逐条等于源文件**：用新 DTO `SrcVfxRt`（`:22704-22711`）**直接数 `Resources/card_vfx_by_card.json`**，与运行时表比（**不写死数字**）。
3. `:11005` ★ 精确绑定里**音频真在工程里的占多数**（`(hit-miss)*2 > hit`），并把缺的名字逐条打进消息。
4. `:11015` ★★ **灭自证①**：至少一条 **精确值 ≠ 后缀兜底值**（现算 12）。
5. `:11017` ★★ **灭自证②**：至少一张卡**只有**精确绑定、兜底结构上给不出（现算 33）。
6. `:11068-11077` **端到端**：现有「打一张有语音的牌」那段之后，若那张卡有可用的精确部署绑定，断 `chat.LastClipName == 那条` ⇒ 证明 `SpeakFor` **真的走精确档**。
> 4 与 5 是**同一对的灭自证**：若有人把精确档改回「照抄后缀兜底」，**两条同时变 0**；5 更强 —— 那 33 张卡**在我们自己的台词表里根本没有**，兜底路径**不可能**产出它们。
> ⚠️ 写这组断言时自己踩过一个坑（已在代码里注明）：兜底那条带 `.ogg`、精确那条**不带扩展名** ⇒ 直接 `!=` 会比出「586 条全不同」的假数；**必须比去扩展名的文件名**（`:10995-11001`）。

### 行为变化（哪些既有行为会变 —— 尤其 `rng=null` 那一档）
- **`rng = null` 那一档没有变化**：`grep -rn "TryPick("` 全仓 **13 处调用点，全部传 `null`** ⇒ 「同一后缀多条时随机挑一条」这条路**本来就是死的**（督军 `gen1..7` 那种池**恒取第一条**）。接精确档后仍是「定死一条」，只是**定死的是哪一条**变了。现核提的「池里挑 → 定死一条」**不成立**。
- 实际变化面：**12 条**换成另一条 clip（例：`BL34`/`BL65`/`TL74` 的 attack/death 由「我们表里挂错卡的那条」换回 SO 那条）· **33 条**从**不说话**变成说话 · **4 条**仍不说话（音频没导进来，`Clip()` 会 `LogWarning` 出声后**退回兜底**）。
- 未动的入口：`SpeakConcede` / `SpeakChat` / `SpeakCantDo` / `SpeakHurry` / 开局独白 / 卡牌详情窗的「听一下」**一律未动**（原版 SO 里没有对应字段 ⇒ **不是**精确档的覆盖范围）。
- `LastClipName` 对精确命中那条**没有扩展名**（`assetName` 就是不带扩展名的原版 AudioClip 名）。`Clip()` 两种都吃；只有日志/断言消息里看着不齐。

### 没查清 / 没做（如实）
- **那 4 条音频没导进工程**（根因见上）⇒ 这 4 张卡**仍然没声音**。修在 `工具/import_original_audio.py`（**不在我的白名单**）⇒ 未动。
- `attackAudio` 那 1 条 `EC56` 被标成 `gen7`（`VO_CSM_EC_Lord Kaphrael_gen7`）—— 原版 SO 就是这么绑的（现核也记了），**为什么**没查。
- `voice_lines.json` 里 **34 条 clip 挂在别的卡名下**（如 `BL4`「Chaos Sergeant」的 clip 被记在 `BL46` 名下）+ **35 张有精确绑定的卡根本不在 `voice_lines.json` 里**。我按「**卡 SO 是权威**」处置（卡名逐条对得上），但**没去查 `card_index.json` 那条链为什么错**。
- `SummonAudio` 与我们的 `ev=line` 是否**恒等**同一件事：无判据（现核已记，我也没查到）。

## A1818 `animateFeel` 断言

### 改了什么 / 断言原文 / 没查清
`CardPresentation/Editor/BattleScene.cs` 的 `CheckSavedScene` 里、`sel`（= `Object.FindObjectOfType<BattleDriver>()`，上面那一族刚取过）之后（`:21586-21594`）：
```
Check(sel != null && sel.animateFeel,
      "★ 存档场景里 `animateFeel == 1`（Scenes/Battle.unity:1054）—— 全仓只有 `BuildAndSaveScene` 会置 true，"
    + "换一条建场路径就会**静默**关掉整层手感（攻击位移 / 命中抖动 / 阵亡消散 / 发牌入场）");
Check(sel != null && sel.mulliganEnabled,
      "★ 存档场景里 `mulliganEnabled == 1`（Scenes/Battle.unity:1050）—— 同一条规矩…");
```
- 判据：`Scenes/Battle.unity:1054 animateFeel: 1`、`:1050 mulliganEnabled: 1`（同组件 `--- !u!114 &388527380`，起于 `:1017`）；写点 `BattleScene.cs:150/152`。
- `mulliganEnabled` 顺手一并加（同一条规矩、同一段、风险相同）。
- ⛔ 只读、零行为改动。**只有跑 `BattleScene.Run` 才走得到**（未跑，见下）。**没查清：无。**

## 顺手发现（只报不判，请调度台分流）
1. 🔴 **`工具/import_original_audio.py` 漏了 4 条卡 SO 引用的语音**（=`card_index.json` 的引用面比卡 SO 小）。判据：这 4 个 `.ogg` **在 `assets_full` 里都在**，只是没拷进 `Resources/Art/audio/vo/`。⇒ 建议立账（改导入脚本的「引用来源」= 卡 SO ∪ `card_index.json`）。
2. **`Core/VfxMap.cs:227` 的文档数字过期**：写「`Resources/card_vfx_by_card.json`（240 KB · 979 张卡 · 2323 条）」，现读 **288.9 KB**（卡数 989 的成因见上）。**`VfxMap.cs` 不在我的白名单** ⇒ 未改（VFX 部分仍是 979/2323，只是文件里多了 10 张只有语音的卡）。
3. **`voice_lines.json`（`card_index.json` 那条链）的归属有错**：34 条 clip 挂在**别的卡**名下（`BL4`→记在 `BL46` · `DA34`→`DA83` · `EC21`→`EC_Terminator` …），另有 **35 张**有精确绑定的卡**整张不在表里**。我这件按「卡 SO 权威」处置了，但**表本身**该不该重导，是一笔新账。
4. `VfxMap.ByCard` 是**静态只增不减**（无 clear）—— 今天无害；**`VoiceLines` 的精确档同理**（`_triedExact` 一次性），第二个消费者要当心。
5. `VoiceLines.Clip()` 的 `_missingLogged` 决定了我这 4 条缺音频**只警告一次**（每 run 一次），批处理里能看见。
6. **验证未做**：本件**没跑 `BattleScene.Run`**（铁律 12 + 简报红线）⇒ 那 6 条新断言**只过了编译器、没过运行时**。按覆盖面，本批覆盖 = **`BattleScene.Run` 一条**；`gen_card_vfx_by_card.py` 与 JSON 是**纯数据/纯工具**（0 条）。

## 落盘（铁律 5，就地更正）
- `资料/普查产出_第二十七会话/战斗动画特效_逐事件现状.md`：§一 结论 6/7/10、§二 总表三处（部署·近战·阵亡的「未接」）、§四 整节（原「没做」三条 + 六步里的 ⑤）、§六·2 —— 全部按「已做 + 更正痕迹（原来写 X / 实际 Y / 错因 Z）」改掉（该文**不是正本**，简报授权可改）。
