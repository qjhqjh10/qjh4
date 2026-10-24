# D8 · 补全 `card_chat_intro` 覆盖（兜底档三项 + 85 条 `_vs_` 复测）

> 执行代理 **D8** · 2026-10-24
> 判据：`资料/普查产出_1024/C5_剩余判据普查.md` §4（`_vs_` 通道 / `GetChatSoundAsset.c:14-45` 四级回退）
> ＋ 卡 SO 全字段语料 `资料/普查产出_1018/allcards_1350/cards_raw.json`（1,351 张，`full_raw`）
> ＋ `guid_map_bundles.tsv` / `guid_map.tsv`（AudioClip 资产名）
> 改动：`工具/gen_card_chat_intro.py`（生成器）· 两份 `card_chat_intro.json`（产物）· 本报告。
> ⛔ 未动任何 `.cs` / `voice_lines.json` / `RuleEngine/**` / 正本 / git；未跑 Unity。

---

## 〇、一句话结论

- **卡覆盖：49 → 56 张运行时卡**（全量件 60 → **67** 张）。新增 7 张**只有兜底档、没有定制档**的卡：
  `EC55 / EC56 / EC57 / GOF76 / GOF77 / SW2 / TL74`。
- 每张卡补出 **`fallbackSounds`**（键名 = 原版字段名）：`introChatSound`（`GetChatSound(Intro)`）·
  `mirrorChatSound`（`GetChatSound(MirrorMatch)`）· `defaultChatSound`（`GetChatSound` 末尾兜底）。
  运行时件 **168 条** / 全量件 **198 条**；GUID → clip 名**全部解出**（`soundGuidsUnresolved = 0`）。
- 🔴 **D7 的前提不成立**：`customIntroChatData` **不是「远不止 49 条」** —— 卡 SO 里就是 **57 张**（216 条，去重 215），
  其中 **49 张**在运行时卡池，**一条都没漏**。真正漏掉的是**兜底档那三项普通开场白**（生成器之前根本没读）。
  （已按铁律 5 在生成器 docstring 就地订正，见 §七-1。）
- **85 条 `_vs_` 未命中数前后不变**（151/236 命中 → 151/236）；但其中 **22 条的「主人卡」本次进了表**
  （「整卡不在表」的 clip 从 30 → 8 条）。要让这 85 条真正「按对手键命中」得动 `voice_lines` / `BattleDriver`
  —— 两者都是本任务黑名单（见 §六-1/§六-4）。

---

## 一、前后读数

| 项 | 前 | 后 | 说明 |
|---|---|---|---|
| 运行时件卡数 | **49** | **56** | +7 张只有兜底档的卡 |
| 全量件卡数 | **60** | **67** | 同上（全量件另有 11 张副本/教程 SO） |
| 运行时件定制档条目 | 197（精确 168 + 阵营 29） | **197** | **不变**（一张卡的定制档都没增删） |
| 全量件定制档条目 | 244（245 含重复） | **244** | 不变 |
| SO 里带 `introChatSound` 的卡 | 66（**未导出**） | **66 导出 / 运行时 56** | 之前表里根本没有这一项 |
| SO 里带 `mirrorChatSound` 的卡 | 66（未导出） | **66 / 56** | 同上 |
| SO 里带 `defaultChatSound` 的卡 | 66（未导出） | **66 / 56** | 同上 |
| `fallbackSounds` 条目 | 0 | **198（运行时 168）** | 66×3 / 56×3 |
| `soundGuids`（全量件） | 190 | **347** | 新增 157 个兜底档 GUID |
| `soundGuidsUnresolved` | 0 | **0** | 两个表都查得到 |
| 文件字节（运行时 / 全量） | 54,640 / 79,118 | **88,110 / 120,160** | indent=1，纯 LF |
| 行数（运行时 / 全量） | 2,017 / 2,725 | **3,113 / 4,035** | |
| `missingKeys`（全量件） | 22 | **22** | 不变（新增的三项走自己的 `hasText`，不混进缺键清单） |

**新入表的 7 张卡**（`introByOpponent`/`introByArmy` 都是 0，只有 `fallbackSounds`）：

| 卡 | 名 | 阵营 | intro clip |
|---|---|---|---|
| EC55 | Lucius the Eternal | EmperorsChildren | `VO_CSM_EC_Lucius_intro` |
| EC56 | Lord Kaphrael | EmperorsChildren | `VO_CSM_EC_Lord Kaphrael_intro` |
| EC57 | Xarahan Daemon Prince | EmperorsChildren | `VO_CSM_EC_Daemon Prince Xarahan_intro` |
| GOF76 | Mozrog Skragbad | Goff | `VO_Ork_Mozrog Skragbad_intro` |
| GOF77 | Beastboss Morgrim | Goff | `VO_Ork_Beastboss Morgrim_intro` |
| SW2 | Njal Stormcaller | SpaceWolves | `VO_SpaceWolves_Njal Stormcaller_intro` |
| TL74 | Terror of Vardenghast | Leviathan | `VO_Tyranid_Leviathan_Winged Tyranid Prime_intro` |

---

## 二、新增字段（schema **只增不改**）

`cards[*].fallbackSounds`（**只写 GUID 非空的那几项**；键不存在 = 这张卡 SO 上没这一条）：

```json
"fallbackSounds": {
  "introChatSound": {
    "key": "Chat_Messages/AM1-Chat0",
    "hasText": true,
    "soundGuid": "558678d7d2b0581418e619ed08838f13",
    "soundAsset": "VO_AM_Arcadian Leontus_intro"
  },
  "mirrorChatSound": {
    "key": "Chat_Messages/AM1-Chat1",
    "hasText": true,
    "soundGuid": "33c0265a7f59a2145a66cccf2b2eb3be",
    "soundAsset": "VO_AM_Arcadian Leontus_mirror"
  },
  "defaultChatSound": {
    "soundGuid": "37d17cf4175da0f429d8f33e05445047",
    "soundAsset": "VO_AM_Arcadian Leontus_attack"
  }
}
```

口径（逐条都有出处，写进生成器 `algorithm.fallbackSound`）：

- **键的拼法**：`introSound`/`mirrorSound` 的文本键走 `RawCardScript.GetLocalizedChatMsg`
  = **`chatMessagesReferenceId ?: uniqueId`** + `-Chat0` / `-Chat1`
  （⚠️ **不看** `customIntroReferenceId` —— 与精确档的 `chatId` 链不同，生成器里用了单独的 `fallback_chat_id`）。
- `defaultChatSound` **没有专属文本键**（它是 `GetChatSound` 的收尾），所以只有 `soundGuid`/`soundAsset`。
- `hasText` 现读 **56/56 全 true**（`-Chat0` 与 `-Chat1` 在官方 en+zh-CN 表里都有原文）。
- **对运行时零影响**：`Battle/UnitChatPanel.cs` 的 `CardChatIntro.EnsureLoaded` 只读
  `id` / `chatId` / `introByOpponent` / `introByArmy`（已逐行读），多出来的键被忽略。
- 现有字段一个没改：49 张老卡的 `id/name/army/chatId/talksFirst/introByOpponent/introByArmy`
  逐字段比对**全等**（脚本核过），只是每张多了一个 `fallbackSounds`。

---

## 三、85 条 `_vs_` 复测（前 → 后）

**口径复现**（与 D7 §三 一致）：`voice_lines.json` 里文件名带 `_vs` 段的 distinct clip = **236**；
按 `card_chat_intro` 的 `soundAsset`（去扩展名）精确匹配 ⇒ **151 命中 / 85 未命中**。

| 指标 | 前 | 后 |
|---|---|---|
| distinct `_vs_` clip | 236 | 236 |
| 命中（`soundAsset` 精确） | **151** | **151** |
| 未命中 | **85** | **85** |
| ├ 未命中里「主人卡整卡不在表」 | **27** | **5** |
| └ 未命中里「主人在表 / 其它」 | 58 | 80 |
| 236 条里「主人卡整卡不在表」（含 3 条归属疑误） | 30 | 8 |
| 主人卡在表里能查到的 clip | 206 | **228** |
| 命中里「主人不是条目主人」（归属疑误） | 3 | 3 |

**为什么命中数不变**：本次新增的三项是「**卡自己的普通开场白**」，不是**对手条目**；
`CardChatIntro.TryResolve` 找的是 `introByOpponent[foeChatId]` / `introByArmy[foeArmy]` ——
新入表的 7 张卡这两档都是 0 条 ⇒ 对它们的 `TryResolve` 仍返回 `false`（行为与「卡不在表」**完全相同**，已核）。

**85 条拆分（后）**：

| 类 | 条数 | 含义 |
|---|---|---|
| **A** 原版 `tbc` 占位 | **11** | 文件名含 `tbc`（To Be Confirmed），原版自己就没真值 |
| **B** 主人卡 D8 新入表、但 0 条定制档 | **22** | EC55×9 · GOF76×5 · GOF77×5 · SW2×3 —— 卡现在在表里，但该卡 SO **没有** `customIntroChatData` |
| **C** 在表，但无该对手条目 | **47** | 卡有定制档，只是没有**这个对手**（含按阵营档也没有） |
| **D** 主人卡仍不在表 | **5** | BL5×2 · BL67×3 —— 这两张连 `introChatSound` 的 GUID 都是空的 |
| 合计 | **85** | |

**另 3 条不是「未命中」，是「归属疑误」**（见 §七-2）：`VO_BlackLegion_Abaddon_vsGrukk/Hexscorn/Orikan.ogg`
在 `voice_lines` 里挂在 **BL5「Hound of Abaddon」**，但条目其实是 **BL34「Abaddon the Despoiler」** 的。

### 85 条逐条（仍未按对手键命中）

| # | 卡 | clip | 现 ev | 卡在表（前→后） | 定制档对手条目 | 归类 |
|---|---|---|---|---|---|---|
| 1 | BL36 | `VO_BlackLegion_Hexscorn_vs_tbc2.ogg` | intro-vs | 有→有 | 4 | A 原版 tbc 占位 |
| 2 | BL36 | `VO_BlackLegion_Hexscorn_vs_tbc3.ogg` | intro-vs | 有→有 | 4 | A 原版 tbc 占位 |
| 3 | BL36 | `VO_BlackLegion_Hexscorn_vs_tbc4.ogg` | intro-vs | 有→有 | 4 | A 原版 tbc 占位 |
| 4 | DA3 | `VO_DA_Asmodai_vsTBC1.ogg` | vstbc1 | 有→有 | 1 | A 原版 tbc 占位 |
| 5 | DA3 | `VO_DA_Asmodai_vsTBC2.ogg` | vstbc2 | 有→有 | 1 | A 原版 tbc 占位 |
| 6 | GOF27 | `VO_Goff_Zagstruk_vsTBC2.ogg` | vstbc2 | 有→有 | 3 | A 原版 tbc 占位 |
| 7 | GOF27 | `VO_Goff_Zagstruk_vsTBC4.ogg` | vstbc4 | 有→有 | 3 | A 原版 tbc 占位 |
| 8 | GOF28 | `VO_Goff_Ghazghkull_vsTBC1.ogg` | vstbc1 | 有→有 | 6 | A 原版 tbc 占位 |
| 9 | SAU37 | `Sautekh_Orikan_vsTBC.ogg` | vstbc | 有→有 | 4 | A 原版 tbc 占位 |
| 10 | UM48 | `VO_Ultramarines_Tigurius_vsTBC1.ogg` | vstbc1 | 有→有 | 3 | A 原版 tbc 占位 |
| 11 | UM48 | `VO_Ultramarines_Tigurius_vsTBC2.ogg` | vstbc2 | 有→有 | 3 | A 原版 tbc 占位 |
| 12 | EC55 | `VO_CSM_EC_Lucius_vsAbaddon.ogg` | vsabaddon | 无→有 | 0 | B D8 新入表（0 条定制档） |
| 13 | EC55 | `VO_CSM_EC_Lucius_vsAstraMilitarum.ogg` | vsastramilitarum | 无→有 | 0 | B D8 新入表（0 条定制档） |
| 14 | EC55 | `VO_CSM_EC_Lucius_vsBile.ogg` | vsbile | 无→有 | 0 | B D8 新入表（0 条定制档） |
| 15 | EC55 | `VO_CSM_EC_Lucius_vsCSM.ogg` | vscsm | 无→有 | 0 | B D8 新入表（0 条定制档） |
| 16 | EC55 | `VO_CSM_EC_Lucius_vsDA.ogg` | vsda | 无→有 | 0 | B D8 新入表（0 条定制档） |
| 17 | EC55 | `VO_CSM_EC_Lucius_vsEC.ogg` | vsec | 无→有 | 0 | B D8 新入表（0 条定制档） |
| 18 | EC55 | `VO_CSM_EC_Lucius_vsFulgrim.ogg` | vsfulgrim | 无→有 | 0 | B D8 新入表（0 条定制档） |
| 19 | EC55 | `VO_CSM_EC_Lucius_vsUM.ogg` | vsum | 无→有 | 0 | B D8 新入表（0 条定制档） |
| 20 | EC55 | `VO_CSM_EC_Lucius_vsWE.ogg` | vswe | 无→有 | 0 | B D8 新入表（0 条定制档） |
| 21 | GOF76 | `VO_Ork_Mozrog Skragbad_vsBL.ogg` | vsbl | 无→有 | 0 | B D8 新入表（0 条定制档） |
| 22 | GOF76 | `VO_Ork_Mozrog Skragbad_vsMorgrim.ogg` | vsmorgrim | 无→有 | 0 | B D8 新入表（0 条定制档） |
| 23 | GOF76 | `VO_Ork_Mozrog Skragbad_vsSwarmlord.ogg` | vsswarmlord | 无→有 | 0 | B D8 新入表（0 条定制档） |
| 24 | GOF76 | `VO_Ork_Mozrog Skragbad_vsTervigon.ogg` | vstervigon | 无→有 | 0 | B D8 新入表（0 条定制档） |
| 25 | GOF76 | `VO_Ork_Mozrog Skragbad_vsUM.ogg` | vsum | 无→有 | 0 | B D8 新入表（0 条定制档） |
| 26 | GOF77 | `VO_Ork_Beastboss Morgrim_vsAstraMilitarum.ogg` | vsastramilitarum | 无→有 | 0 | B D8 新入表（0 条定制档） |
| 27 | GOF77 | `VO_Ork_Beastboss Morgrim_vsDarkAngels.ogg` | vsdarkangels | 无→有 | 0 | B D8 新入表（0 条定制档） |
| 28 | GOF77 | `VO_Ork_Beastboss Morgrim_vsMozrog.ogg` | vsmozrog | 无→有 | 0 | B D8 新入表（0 条定制档） |
| 29 | GOF77 | `VO_Ork_Beastboss Morgrim_vsShadowsun.ogg` | vsshadowsun | 无→有 | 0 | B D8 新入表（0 条定制档） |
| 30 | GOF77 | `VO_Ork_Beastboss Morgrim_vsTyranids.ogg` | vstyranids | 无→有 | 0 | B D8 新入表（0 条定制档） |
| 31 | SW2 | `VO_SpaceWolves_Njal Stormcaller_vsDG.ogg` | vsdg | 无→有 | 0 | B D8 新入表（0 条定制档） |
| 32 | SW2 | `VO_SpaceWolves_Njal Stormcaller_vsTS.ogg` | vsts | 无→有 | 0 | B D8 新入表（0 条定制档） |
| 33 | SW2 | `VO_SpaceWolves_Njal Stormcaller_vsWE.ogg` | vswe | 无→有 | 0 | B D8 新入表（0 条定制档） |
| 34 | AM2 | `VO_AM_Ursula Creed_vsAbaddon.ogg` | vsabaddon | 有→有 | 2 | C 在表，无该对手条目 |
| 35 | AM3 | `VO_AM_Commissar Elan_vsMinka.ogg` | vsminka | 有→有 | 5 | C 在表，无该对手条目 |
| 36 | AM66 | `VO_AM_Hektor Thenmann_vsTau.ogg` | vstau | 有→有 | 3 | C 在表，无该对手条目 |
| 37 | ASH74 | `VO_CW_SaimHann_Jain Zar_vsDrukhari.ogg` | vsdrukhari | 有→有 | 3 | C 在表，无该对手条目 |
| 38 | ASH74 | `VO_CW_SaimHann_Jain Zar_vsKarandras.ogg` | vskarandras | 有→有 | 3 | C 在表，无该对手条目 |
| 39 | BL35 | `VO_BlackLegion_Haarken_vs_JainZar.ogg` | intro-vs | 有→有 | 5 | C 在表，无该对手条目 |
| 40 | BL36 | `VO_BlackLegion_Hexscorn_vs_UrsulaCreed.ogg` | intro-vs | 有→有 | 4 | C 在表，无该对手条目 |
| 41 | DA1 | `VO_DA_Azrael_vsAbaddon.ogg` | vsabaddon | 有→有 | 1 | C 在表，无该对手条目 |
| 42 | DA3 | `VO_DA_Asmodai_vsAzrael.ogg` | vsazrael | 有→有 | 1 | C 在表，无该对手条目 |
| 43 | DA3 | `VO_DA_Asmodai_vsOrksTyranids.ogg` | vsorkstyranids | 有→有 | 1 | C 在表，无该对手条目 |
| 44 | GOF28 | `VO_Goff_Ghazghkull_vsKnightTitan.ogg` | vsknighttitan | 有→有 | 6 | C 在表，无该对手条目 |
| 45 | GOF28 | `VO_Goff_Ghazghkull_vsMinka.ogg` | vsminka | 有→有 | 6 | C 在表，无该对手条目 |
| 46 | GOF75 | `VO_Ork_Goff_Gordrang_vsOMaisos.ogg` | vsomaisos | 有→有 | 4 | C 在表，无该对手条目 |
| 47 | GSC1 | `VO_GSC_Saffa Rhiannor_vsAeldari.ogg` | vsaeldari | 有→有 | 2 | C 在表，无该对手条目 |
| 48 | GSC1 | `VO_GSC_Saffa Rhiannor_vsSpaceMarine.ogg` | vsspacemarine | 有→有 | 2 | C 在表，无该对手条目 |
| 49 | GSC2 | `VO_GSC_Uthrel Naas_vsAstraMilitarum.ogg` | vsastramilitarum | 有→有 | 2 | C 在表，无该对手条目 |
| 50 | GSC2 | `VO_GSC_Uthrel Naas_vsSpaceMarine.ogg` | vsspacemarine | 有→有 | 2 | C 在表，无该对手条目 |
| 51 | GSC3 | `VO_GSC_Acolyte Iconward_vsAstraMilitarum.ogg` | vsastramilitarum | 有→有 | 3 | C 在表，无该对手条目 |
| 52 | SAU36 | `Sautekh_Zahndrekh_vs_Tau.ogg` | intro-vs | 有→有 | 4 | C 在表，无该对手条目 |
| 53 | SAU37 | `Sautekh_Orikan_vsCadia.ogg` | vscadia | 有→有 | 4 | C 在表，无该对手条目 |
| 54 | SAU37 | `Sautekh_Orikan_vsTrazyn.ogg` | vstrazyn | 有→有 | 4 | C 在表，无该对手条目 |
| 55 | SAU66 | `VO_Sautekh_Ramatekh the Cruel_vsNecron.ogg` | vsnecron | 有→有 | 5 | C 在表，无该对手条目 |
| 56 | SOR1 | `VO_ADS_Erika Luminas_vsHesperax1.ogg` | vshesperax1 | 有→有 | 5 | C 在表，无该对手条目 |
| 57 | SOR1 | `VO_ADS_Erika Luminas_vsHesperax2.ogg` | vshesperax2 | 有→有 | 5 | C 在表，无该对手条目 |
| 58 | SOR2 | `VO_ADS_Junith Eruita_vsAstraMilitarum.ogg` | vsastramilitarum | 有→有 | 4 | C 在表，无该对手条目 |
| 59 | SOR3 | `VO_ADS_Morvenn Vahl_vsAstraMilitarum.ogg` | vsastramilitarum | 有→有 | 6 | C 在表，无该对手条目 |
| 60 | SOR65 | `VO_ADS_Celestian Sacresant Aveline_vsDaemon.ogg` | vsdaemon | 有→有 | 1 | C 在表，无该对手条目 |
| 61 | SOR65 | `VO_ADS_Celestian Sacresant Aveline_vsNL.ogg` | vsnl | 有→有 | 1 | C 在表，无该对手条目 |
| 62 | SOR65 | `VO_ADS_Celestian Sacresant Aveline_vsSylar.ogg` | vssylar | 有→有 | 1 | C 在表，无该对手条目 |
| 63 | SW3 | `VO_SpaceWolves_Ragnar Blackmane_vsDG.ogg` | vsdg | 有→有 | 2 | C 在表，无该对手条目 |
| 64 | SW3 | `VO_SpaceWolves_Ragnar Blackmane_vsTS.ogg` | vsts | 有→有 | 2 | C 在表，无该对手条目 |
| 65 | TAU1 | `VO_Tau_Empire_Shadowsun_vsImperial.ogg` | vsimperial | 有→有 | 4 | C 在表，无该对手条目 |
| 66 | TAU1 | `VO_Tau_Empire_Shadowsun_vsKroot.ogg` | vskroot | 有→有 | 4 | C 在表，无该对手条目 |
| 67 | TAU2 | `VO_Tau_Empire_Aun'va_vsAstraMillitarum.ogg` | vsastramillitarum | 有→有 | 5 | C 在表，无该对手条目 |
| 68 | TAU2 | `VO_Tau_Empire_Aun'va_vsHexscorn.ogg` | vshexscorn | 有→有 | 5 | C 在表，无该对手条目 |
| 69 | TAU2 | `VO_Tau_Empire_Aun'va_vsKrootShaper.ogg` | vskrootshaper | 有→有 | 5 | C 在表，无该对手条目 |
| 70 | TAU3 | `VO_Tau_Empire_Commander O'Maisos_vsSM.ogg` | vssm | 有→有 | 6 | C 在表，无该对手条目 |
| 71 | TAU65 | `VO_Tau_War Shaper_vsUrsulaCreed.ogg` | vsursulacreed | 有→有 | 1 | C 在表，无该对手条目 |
| 72 | UM46 | `VO_UM_Calgar_vsAgemman.ogg` | vsagemman | 有→有 | 6 | C 在表，无该对手条目 |
| 73 | UM46 | `VO_UM_Calgar_vsMkar.ogg` | vsmkar | 有→有 | 6 | C 在表，无该对手条目 |
| 74 | UM48 | `VO_Ultramarines_Tigurius_vsGuilliman.ogg` | vsguilliman | 有→有 | 3 | C 在表，无该对手条目 |
| 75 | UM48 | `VO_Ultramarines_Tigurius_vsMKar.ogg` | vsmkar | 有→有 | 3 | C 在表，无该对手条目 |
| 76 | UM87 | `VO_UM_Guilliman_vsFulgrim.wav` | vsfulgrim | 有→有 | 4 | C 在表，无该对手条目 |
| 77 | UM87 | `VO_UM_Guilliman_vsLion.wav` | vslion | 有→有 | 4 | C 在表，无该对手条目 |
| 78 | UM87 | `VO_UM_Guilliman_vsMortarion.wav` | vsmortarion | 有→有 | 4 | C 在表，无该对手条目 |
| 79 | UM87 | `VO_UM_Guilliman_vsUM.wav` | vsum | 有→有 | 4 | C 在表，无该对手条目 |
| 80 | UM88 | `VO_UM_Valius Paxor_vsUM.wav` | vsum | 有→有 | 0 | C 在表，无该对手条目 |
| 81 | BL5 | `VO_BlackLegion_Abaddon_vsCalgar.ogg` | vscalgar | 无→无 | -1 | D 主人卡仍不在表 |
| 82 | BL5 | `VO_BlackLegion_Abaddon_vsMinka.ogg` | vsminka | 无→无 | -1 | D 主人卡仍不在表 |
| 83 | BL67 | `VO_BL_Ghallaron_vsGhazghkull.ogg` | vsghazghkull | 无→无 | -1 | D 主人卡仍不在表 |
| 84 | BL67 | `VO_BL_Ghallaron_vsKeltoc.ogg` | vskeltoc | 无→无 | -1 | D 主人卡仍不在表 |
| 85 | BL67 | `VO_BL_Ghallaron_vsVarroTigurius.ogg` | vsvarrotigurius | 无→无 | -1 | D 主人卡仍不在表 |

---

## 四、幂等读数

```
完整生成 2 次：
  run1  sha256 = bfd647a2a375ab2a7d3fb2fcb35a3054977ef3622130d478c23002a4cf554519  (数据/游戏数据/card_chat_intro.json)
               b9afd08cc0c621549514af632cf4051b7dd1a47c5fd5983dff0de2e51694ab5a  (Resources/card_chat_intro.json)
  run2  sha256 = 同 run1 两行逐字相同  ← 幂等 ✅
--check：两个文件都「一致 ✅」，rc = 0
控制台两次逐字相同（SO 卡 67 / 运行时 56 / 精确档 215 / 阵营 29 / 键 197 / 兜底档 168）
行尾：生成器 + 两份 JSON 都是**纯 LF**（CRLF 计数 0，与改前一致）
.meta：已存在 ⇒ 未重写（生成器只在缺失时建）
音频：未拷、未删（这是纯数据生成，不碰 Resources/Art）
```

---

## 五、改动清单

### 5.1 `工具/gen_card_chat_intro.py`（+127 / −12，纯 LF）

1. 新增 `FALLBACK_SOUND_FIELDS`（三项字段名 + 文本键后缀）与 `_field_guid()`（读 `m_AssetGUID`；
   ⚠️ 注释写清：这些字段**总是**一个三键 dict，空值表现为 `m_AssetGUID == ""`，
   **不能**拿 `_ne(dict)` 判非空 —— 我第一版就是这么误数成「1351 张都有」的）。
2. 选卡条件加三项兜底档（有 GUID 就入选）⇒ 60 → 67 张。
3. 每卡算 `fallback_chat_id = chatMessagesReferenceId ?: uniqueId`，产出 `fallbackSounds`。
4. `counts` 新增 `fallbackSoundEntries` / `fallbackSoundEntriesRuntime` /
   `cardsWithIntroChatSound|Mirror|Default`（值是 `[全量, 运行时]`）；运行时件 `counts` 同步加。
5. `algorithm` 新增 `fallbackSound`（四级回退的判定原文）与 `fallbackSoundConsumers`
   （明写「当前 `.cs` **还没读**这个键」）；`note` 两份都补一句。
6. 打印一行兜底档统计；docstring 的「四项 / 60 / 49」就地订正（铁律 5）。

### 5.2 两份产物

- `MyGame/Assets/CardPresentation/Resources/card_chat_intro.json`（运行时件，56 张）
- `数据/游戏数据/card_chat_intro.json`（全量件，67 张）
- 差异只有：`note`/`counts`/`algorithm` 那几行 + 每卡 `fallbackSounds` + 7 张新卡；
  **没有任何老卡的既有字段变动**（脚本逐字段比对，`changed other fields: []`）。
- `git diff --numstat`：`1133/37`（运行时件）· `1320/10`（全量件）—— 与「每卡十几行新增」相符，不是整篇翻行尾。

### 5.3 未改动

`.cs` 全部 · `Resources/voice_lines.json` · `RuleEngine/**` · `Resources/Art/**` · 正本 · git。

---

## 六、没查清 / 没做

1. 🔴 **有一条 `.cs` 自检会因为本次扩容变红（黑名单，未改）**：
   `Editor/BattleScene.cs:19107-19108` 写死
   `Check(CardChatIntro.Loaded && CardChatIntro.Count == 49, "…（{Count} 张，预期 49）")`
   ⇒ 现在 `Count = 56`，这条**必然红**。修法 = 把 `49` 改成 `56`（或改成 `>= 49`）。
   **其余相关断言我按 JSON 逐条复算过，全部仍成立**：
   `EntriesWithSound == 197` ✅（新增键不计入）· `SoundUnresolvedEntries == 0` ✅ ·
   `TryResolve("AM1","BL34")` ✅ true · `TryResolve("AM2","UM46")` ✅ true · `TryResolve("AM1","UM46")` ✅ false ·
   `PlayerTalksFirst(UM46,GOF28)==1` / `(GOF27,BL35)==0` / `(AM1,UM46)==1` ✅。
2. 两处 `.cs` 注释过期（未改）：`Battle/UnitChatPanel.cs:1375`「49 张运行时卡」、`:1456`「运行时件里 49 张的 chatRef/introRef 全空」——
   数字应为 **56**（两者仍都全空，结论不变）。
3. **`fallbackSounds` 目前没有任何消费方**（`.cs` 黑名单）。但它**不是空转**：§七-3 证明运行时已经通过
   `voice_lines` 的 `ev=intro/default` 拿到**同样的 clip**，所以这一项现在的用途是「权威投影 + 对账」。
4. 🔴 **85 条里「会播错」的那部分没修**：`VoiceLines.ForVersus` 的第一跳是 `vs~<对手名>`（按文件名猜的行），
   `intro` 排在第三跳 ⇒ 只要 `voice_lines` 里有那条猜的 `vs*` 行，它**仍然优先于**原版真兜底 `introChatSound`。
   要修得动 `voice_lines.json`（清猜的行）或 `BattleDriver.SpeakIntro`（先按 SO 判定），**两者都在黑名单**。
   这条是 D7 §六-2 的延续，现状**未变**。
5. **BL5 / BL67 没有任何开场白音频可导**：三项 GUID 现读**全空**（不是「没找到」）。
6. **7 个查不到的 `opponentWarlord` GUID**（`66754335…`/`c9d0f799…`/`b9d9416b…`/`13b690f8…`/`1541330c…`/`3ff2bfaa…`/`67e9f01a…`）：
   只出现在 `Marneus Calgar (tutorial)` / `Uriel Ventris (tutorial)` 两个**非运行时** SO 里。
   我搜过的地方：`cards_raw.json` 全 1,351 张（uniqueId / chatMessagesReferenceId / customIntroReferenceId 三张表）、
   `cards_engine.json`、整个 `d:/4` 的 `*.json/*.tsv/*.md`。**没有解出它们是谁**，保留原样（⛔ 没猜）。
7. **没跑 Unity**（红线）。C# 解析器兼容性用「逐行读 `EnsureLoaded` + Python 复算全部相关断言」代替。
8. **没做**真机/音频实际播放验证（属真 Play，不在本任务范围）。

---

## 七、顺手发现

1. 🔴 **D7 §六-3 的前提是错的**（已在生成器 docstring 就地订正，铁律 5）：
   它说「`customIntroChatData` 挂在**所有** `RawCardScript` 上 …… 这些单位在我们的 `card_chat_intro.json` 里没有条目」。
   现读：**只有 57 张** SO 有 `customIntroChatData`（`Hound of Abaddon` / `Celestian Sacresant Aveline` **都没有**），
   其中 49 张在运行时卡池。所以「扩到 1351 张」这件事**没有数据可扩** —— 真正缺的是兜底档三项。
2. 🔴 **`voice_lines.json` 有 3 条 `_vs_` 的主人标错**（未动，黑名单）：
   `VO_BlackLegion_Abaddon_vsGrukk.ogg` / `…_vsHexscorn.ogg` / `…_vsOrikan.ogg` 现在挂在 **BL5「Hound of Abaddon」**，
   而 `card_chat_intro` 的 `soundAsset` 证明它们就是 **BL34「Abaddon the Despoiler」** 的
   `introByOpponent` 条目（对手分别 `GOF29`/`BL36`/`SAU37`）。错因应是 `_vs_` 主人规则按「卡名含 Abaddon」匹配，
   把「Hound of Abaddon」也算进去了。修了之后 **BL5 的孤儿会从 5 条降到 2 条**（那 2 条才是真孤儿）。
3. 🟢 **运行时其实早就有正确的开场白兜底**：`voice_lines.json` 里 **56 张卡**各有 `ev=intro` / `ev=mirror` / `ev=default`
   各一行（带 `so=introChatSound` 等留痕），clip 与本次导出的 `fallbackSounds` **逐条 1:1 相同**，
   卡集合也**完全一致**（56 = 56，双向差集都是 0）；`VoiceLines.ForVersus` / `ForMirror` 的末两跳就是它们。
   ⇒ 本次新增数据的价值在「**哪 56 张卡有真兜底**」这个权威投影（以前表里只有 49 张，会让人以为另外 7 张没开场白）。
4. 🟢 **每张卡的 `defaultChatSound` GUID 就等于它的 `attackAudio` GUID**（例 `SW2` → `…_attack` 同一 GUID）。
   原版就是共用同一条录音。⛔ 别把它当「GUID 解错了」去改。
5. 🟢 **文本键全有原文**：`-Chat0`（Intro）与 `-Chat1`（MirrorMatch）在官方 en+zh-CN 表里 56/56 全 `hasText=true`
   —— 与 `C5` 里「官方表 `-Chat1-<第三段>` 一条都没有」并不矛盾：那是**带对手键的第三段**，这里是**裸 `-Chat1`**。
6. 🟢 生成器可复用的一个坑（已写进代码注释）：`AssetReferenceTyped<T>` 在 `full_raw` 里**永远是三键 dict**，
   空值 = `m_AssetGUID: ""`。用「dict 非空」判「这卡有没有这个音」会**全部误判为有**
   （我第一版数出「1,351 张都有 introChatSound」，错得还很像真的）。
