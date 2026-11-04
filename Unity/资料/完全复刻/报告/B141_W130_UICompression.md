# B141 · W130 · 15-09 UI 切片压缩档

> 执行写手 `B141-W40` · 会话 128 · 2026-11-04
> 白名单：**只改了** `MyGame/Assets/CardPresentation/Resources/Art/{ui,ui_menu,ui_deck}/**/*.png.meta`（每份只动 `platformSettings` 里 4 个键）+ 本报告。
> ⛔ 没跑 Unity · ⛔ 没动 git · ⛔ 没碰 `d:/2` · ⛔ 没碰账 / 活状态表 / 所有权表 / `ArtBaker.cs` / 任何 `.png` / 任何 arena 纹理。

## 0 · 一句话结果

**精确名单查出来了，不是猜的**：把 `Resources/Art/{ui,ui_menu,ui_deck}` 的 **676 份 `.meta`** 逐个对 `_tmp_view/b139_i/orig_textures.json`（3,009 个原版 `Texture2D`，2,984 唯一名）——**234 份按名对回原版**（其中 **233/234 的 PNG 尺寸与原版 `m_Width×m_Height` 逐张相等**，另一份是已知的 15-08 `40K_shop_offer_bg_SW`）。

按原版 `m_TextureFormat` 逐张改 `.meta`：**改了 137 张**（= 判据口径的 71 张「原名精确匹配」+ 66 张「下划线↔空格改名匹配」；后者是工程自己的落盘惯例 `name.replace(' ', '_')`，见 `工具/import_original_art.py`）。原版混档分布（这 137 张）：**DXT5Crunched 64 · DXT5 40 · RGBA32 25 · DXT1Crunched 6 · RGB24 1 · DXT1 1**。

⚠️ **关键交付风险（调度台必须看一眼）**：`ArtBaker.ApplyImportSettings()` 下次烘焙会把 `Resources/Art` 下**非表内**贴图一律写回 `CompressedHQ` ⇒ **本轮的 137 张会在下次 `ArtBaker.BakeFromCLI` 时被静默打回**。判据表在 §6，需 ArtBaker 的写手（W1）把它并进 `CompTruths`（并补一个 Crunch 字段），否则今天这次改动不可持续。

---

## 1 · 判据与方法

- 判据源：`判据/B141_W130_对账_资源.md` §一 15-09（R9）· `报告/B138_审计汇总_问题清单.md` §15 15-09 · `报告/B136_审计_纹理与图集.md` #7。
- 原版真值：`_tmp_view/b139_i/orig_textures.json`（`B139_I` 2026-11-04 用 UnityPy 直读 84 个 `StandaloneWindows64/*.bundle` 的缓存件，3,009 对象 / 2,984 唯一名；`m_TextureFormat` / `m_Width` / `m_Height` / `m_MipCount` 全在）。**本轮没有重跑 UnityPy**（缓存同口径可直接用）。
- 我们现值：`_tmp_view/b139_i/our_metas.json`（同批解析的 4,692 份 TextureImporter `.meta`）+ **改前现场逐份 `io.open("rb")` 复核**。

**名称匹配（两级，都要求「原版名 → 落盘名」的工程惯例）**：

| 级 | 规则 | 命中文件 | 说明 |
|---|---|---|---|
| 精确 | 我们的文件名 == 原版 `Texture2D.m_Name` | **138** | 与原判据「138 张可对回原版名」**逐数吻合** |
| 改名 | 我们的文件名 == 原版名 `.replace(' ', '_')` | **96** | 工程落盘惯例（`import_original_art.py` 全文用 `name.replace(' ', '_')`）；**96/96 的 PNG 尺寸与原版逐张相等** |

> **尺寸校验是配对的硬证据**：234 份命中的 PNG 里 **233 份**与原版 `m_Width×m_Height` 完全相等；唯一不等的是 `40K_shop_offer_bg_SW`（450×512，原版 512²）—— 那是 15-08 的已知问题，与本条无关。⇒ 不存在「按名撞上另一张同尺寸图」的系统性风险（原版 2,984 唯一名无同名异格式）。

**改哪些**：原版 `m_TextureFormat != 25`(BC7) 且我们现值 `DefaultTexturePlatform.textureCompression != 1` 的。BC7（81 份）现状 `CompressedHQ` **已同档，不改**；13 份 `40K_shop_offer_bg_*` + 2 份改名版 `textureCompression: 1`（15-04 已处理，不改）。

---

## 2 · 原版混档分布（234 份按名对回的）

| 原版 `m_TextureFormat` | 含义 | 文件数 | 本轮处理 |
|---|---|---|---|
| 3 | RGB24 | 1 | **改 1 张** |
| 4 | RGBA32 | 26 | **改 25 张**（另 1 张 `Noise_Combined` 按指令留给 W1） |
| 10 | DXT1 | 1 | **改 1 张** |
| 12 | DXT5 | 55 | **改 40 张**（另 15 张是 15-04 的 shop_bg，已是 `Compressed`） |
| 25 | BC7 | 81 | 不改（现状 `CompressedHQ` = BC7，同档） |
| 28 | DXT1Crunched | 6 | **改 6 张** |
| 29 | DXT5Crunched | 64 | **改 64 张** |
| **合计** | — | **234** | **改 137 / 不须改 97** |

> 口径说明：判据行原写「71 张（uncomp 15 / DXT5 31 / DXT5C 24 / DXT1C 1）」，那是 **精确匹配级** 的差集，**本轮复算与它逐数吻合**（71）。本轮把「改名级」的 66 张也纳入（同一原版图、同尺寸、同一落盘惯例），故实际改动 = **137**。两组在 §3 分开列，调度台若只认 71，可用备份把 B 组回滚（备份见 §5）。

---

## 3 · 逐张改动清单（137 张）

统一改法（`.meta` 的 `platformSettings`，两个块）：

- **DefaultTexturePlatform**：`textureFormat` / `textureCompression` / `crunchedCompression` 按原版；`overridden` 保持 0。
- **Standalone**：同三键；**有显式格式时 `overridden: 1`**（否则这一块是死值），`Uncompressed` 时保持 0。
- 改前两个块恒为：Default `(textureFormat -1, textureCompression 2, crunchedCompression 0, overridden 0)` · Standalone `(-1, 1, 0, 0)`。

### A1 · 原版未压缩（RGBA32）→ `Uncompressed` —— 25 张

改后：Default `(textureFormat -1, textureCompression 0, crunchedCompression 0, overridden 0)` · Standalone `(-1, 0, 0, overridden 0)`

| # | 落盘文件（`Resources/Art/…`） | 原版名 | 原版 fmt | 匹配级 | 尺寸 |
|---|---|---|---|---|---|
| 1 | `ui/40k_popup_texture.png.meta` | `40k_popup_texture` | 4·RGBA32 | 精确 | 128×128 |
| 2 | `ui/40k_voicelines_radio_wave_equalizer.png.meta` | `40k_voicelines_radio_wave equalizer` | 4·RGBA32 | 改名 | 708×96 |
| 3 | `ui_deck/2_40k_cardframe_rarity_rare.png.meta` | `2_40k_cardframe_rarity_rare` | 4·RGBA32 | 精确 | 84×76 |
| 4 | `ui_deck/40k_DeckSelection_icon_FactionBlackLegion.png.meta` | `40k_DeckSelection_icon_FactionBlackLegion` | 4·RGBA32 | 精确 | 256×256 |
| 5 | `ui_deck/40k_DeckSelection_icon_FactionDarkAngels.png.meta` | `40k_DeckSelection_icon_FactionDarkAngels` | 4·RGBA32 | 精确 | 256×256 |
| 6 | `ui_deck/40k_DeckSelection_icon_FactionUM.png.meta` | `40k_DeckSelection_icon_FactionUM` | 4·RGBA32 | 精确 | 256×256 |
| 7 | `ui_deck/Card_Text_smooth_background.png.meta` | `Card Text smooth background` | 4·RGBA32 | 改名 | 32×32 |
| 8 | `ui_deck/Smooth_background_square.png.meta` | `Smooth background square` | 4·RGBA32 | 改名 | 32×32 |
| 9 | `ui_menu/40K_ArmyTrack_bar_position.png.meta` | `40K_ArmyTrack_bar_position` | 4·RGBA32 | 精确 | 64×64 |
| 10 | `ui_menu/40k_Generic_Smooth_line.png.meta` | `40k_Generic Smooth line` | 4·RGBA32 | 改名 | 113×32 |
| 11 | `ui_menu/40k_battle_Win_Skull_SDF.png.meta` | `40k_battle_Win Skull SDF` | 4·RGBA32 | 改名 | 128×128 |
| 12 | `ui_menu/40k_popup_texture.png.meta` | `40k_popup_texture` | 4·RGBA32 | 精确 | 128×128 |
| 13 | `ui_menu/40k_shop_popup_info_bg.png.meta` | `40k_shop_popup_info_bg` | 4·RGBA32 | 精确 | 565×588 |
| 14 | `ui_menu/40k_topmarquee_currency_blackstone.png.meta` | `40k_topmarquee_currency_blackstone` | 4·RGBA32 | 精确 | 90×90 |
| 15 | `ui_menu/40k_topmarquee_currency_crystal.png.meta` | `40k_topmarquee_currency_crystal` | 4·RGBA32 | 精确 | 90×90 |
| 16 | `ui_menu/40k_topmarquee_currency_energy.png.meta` | `40k_topmarquee_currency_energy` | 4·RGBA32 | 精确 | 90×90 |
| 17 | `ui_menu/40k_topmarquee_currency_gold.png.meta` | `40k_topmarquee_currency_gold` | 4·RGBA32 | 精确 | 90×90 |
| 18 | `ui_menu/40k_topmarquee_currency_ticket.png.meta` | `40k_topmarquee_currency_ticket` | 4·RGBA32 | 精确 | 90×90 |
| 19 | `ui_menu/Background.png.meta` | `Background` | 4·RGBA32 | 精确 | 32×32 |
| 20 | `ui_menu/Crate_Border_Highlight.png.meta` | `Crate Border Highlight` | 4·RGBA32 | 改名 | 128×128 |
| 21 | `ui_menu/Glow_UI_W40K.png.meta` | `Glow UI W40K` | 4·RGBA32 | 改名 | 123×123 |
| 22 | `ui_menu/InputFieldBackground.png.meta` | `InputFieldBackground` | 4·RGBA32 | 精确 | 32×32 |
| 23 | `ui_menu/OctagonUI_Border_SDF_2.png.meta` | `OctagonUI Border SDF 2` | 4·RGBA32 | 改名 | 128×128 |
| 24 | `ui_menu/Tutorial_Highlight.png.meta` | `Tutorial Highlight` | 4·RGBA32 | 改名 | 128×128 |
| 25 | `ui_menu/WF_icon_clock_for_shader.png.meta` | `WF_icon_clock for shader` | 4·RGBA32 | 改名 | 64×64 |

### A2 · 原版未压缩（RGB24）→ `Uncompressed` —— 1 张

改后：Default `(textureFormat -1, textureCompression 0, crunchedCompression 0, overridden 0)` · Standalone `(-1, 0, 0, overridden 0)`

| # | 落盘文件（`Resources/Art/…`） | 原版名 | 原版 fmt | 匹配级 | 尺寸 |
|---|---|---|---|---|---|
| 1 | `ui_menu/40k_Rewards_icon_base_card_SDF.png.meta` | `40k_Rewards_icon_base card SDF` | 3·RGB24 | 改名 | 128×128 |

### B · 原版 DXT5（未 Crunch）→ `Compressed` + 显式 DXT5 —— 40 张

改后：Default `(textureFormat 12, textureCompression 1, crunchedCompression 0, overridden 0)` · Standalone `(12, 1, 0, overridden 1)`

| # | 落盘文件（`Resources/Art/…`） | 原版名 | 原版 fmt | 匹配级 | 尺寸 |
|---|---|---|---|---|---|
| 1 | `ui/CrosshairTrail.png.meta` | `CrosshairTrail` | 12·DXT5 | 精确 | 64×64 |
| 2 | `ui_menu/01-Rook.png.meta` | `01-Rook` | 12·DXT5 | 精确 | 512×512 |
| 3 | `ui_menu/02-Veteran.png.meta` | `02-Veteran` | 12·DXT5 | 精确 | 512×512 |
| 4 | `ui_menu/03-Commander.png.meta` | `03-Commander` | 12·DXT5 | 精确 | 512×512 |
| 5 | `ui_menu/04-Admiral.png.meta` | `04-Admiral` | 12·DXT5 | 精确 | 512×512 |
| 6 | `ui_menu/05-Conqueror.png.meta` | `05-Conqueror` | 12·DXT5 | 精确 | 512×512 |
| 7 | `ui_menu/06-Galactic_Threat.png.meta` | `06-Galactic Threat` | 12·DXT5 | 改名 | 512×512 |
| 8 | `ui_menu/07-Legend.png.meta` | `07-Legend` | 12·DXT5 | 精确 | 512×512 |
| 9 | `ui_menu/Alliance_Trophies_AM.png.meta` | `Alliance_Trophies_AM` | 12·DXT5 | 精确 | 1024×1024 |
| 10 | `ui_menu/Alliance_Trophies_Black Legion event 1.png.meta` | `Alliance_Trophies_Black Legion event 1` | 12·DXT5 | 精确 | 1024×1024 |
| 11 | `ui_menu/Alliance_Trophies_BlackLegion.png.meta` | `Alliance_Trophies_BlackLegion` | 12·DXT5 | 精确 | 1024×1024 |
| 12 | `ui_menu/Alliance_Trophies_Dark Angels.png.meta` | `Alliance_Trophies_Dark Angels` | 12·DXT5 | 精确 | 1024×1024 |
| 13 | `ui_menu/Alliance_Trophies_Default_v2.png.meta` | `Alliance_Trophies_Default_v2` | 12·DXT5 | 精确 | 1024×1024 |
| 14 | `ui_menu/Alliance_Trophies_Emperors Children.png.meta` | `Alliance_Trophies_Emperors Children` | 12·DXT5 | 精确 | 1024×1024 |
| 15 | `ui_menu/Alliance_Trophies_GSC.png.meta` | `Alliance_Trophies_GSC` | 12·DXT5 | 精确 | 1024×1024 |
| 16 | `ui_menu/Alliance_Trophies_Goff.png.meta` | `Alliance_Trophies_Goff` | 12·DXT5 | 精确 | 1024×1024 |
| 17 | `ui_menu/Alliance_Trophies_Leviathan.png.meta` | `Alliance_Trophies_Leviathan` | 12·DXT5 | 精确 | 1024×1024 |
| 18 | `ui_menu/Alliance_Trophies_Ork.png.meta` | `Alliance_Trophies_Ork` | 12·DXT5 | 精确 | 1024×1024 |
| 19 | `ui_menu/Alliance_Trophies_SaimHann.png.meta` | `Alliance_Trophies_SaimHann` | 12·DXT5 | 精确 | 1024×1024 |
| 20 | `ui_menu/Alliance_Trophies_Sautekh.png.meta` | `Alliance_Trophies_Sautekh` | 12·DXT5 | 精确 | 1024×1024 |
| 21 | `ui_menu/Alliance_Trophies_Sororitas.png.meta` | `Alliance_Trophies_Sororitas` | 12·DXT5 | 精确 | 1024×1024 |
| 22 | `ui_menu/Alliance_Trophies_Space Marine 2 event.png.meta` | `Alliance_Trophies_Space Marine 2 event` | 12·DXT5 | 精确 | 512×512 |
| 23 | `ui_menu/Alliance_Trophies_Space Wolves.png.meta` | `Alliance_Trophies_Space Wolves` | 12·DXT5 | 精确 | 1024×1024 |
| 24 | `ui_menu/Alliance_Trophies_TIER1.png.meta` | `Alliance_Trophies_TIER1` | 12·DXT5 | 精确 | 1024×1024 |
| 25 | `ui_menu/Alliance_Trophies_TIER2.png.meta` | `Alliance_Trophies_TIER2` | 12·DXT5 | 精确 | 1024×1024 |
| 26 | `ui_menu/Alliance_Trophies_TIER3.png.meta` | `Alliance_Trophies_TIER3` | 12·DXT5 | 精确 | 1024×1024 |
| 27 | `ui_menu/Alliance_Trophies_TIER4.png.meta` | `Alliance_Trophies_TIER4` | 12·DXT5 | 精确 | 1024×1024 |
| 28 | `ui_menu/Alliance_Trophies_Tau.png.meta` | `Alliance_Trophies_Tau` | 12·DXT5 | 精确 | 1024×1024 |
| 29 | `ui_menu/Alliance_Trophies_Ultramarines event.png.meta` | `Alliance_Trophies_Ultramarines event` | 12·DXT5 | 精确 | 1024×1024 |
| 30 | `ui_menu/Alliance_Trophies_Ultramarines.png.meta` | `Alliance_Trophies_Ultramarines` | 12·DXT5 | 精确 | 1024×1024 |
| 31 | `ui_menu/Copy@3x.png.meta` | `Copy@3x` | 12·DXT5 | 精确 | 48×48 |
| 32 | `ui_menu/LightningTrail.png.meta` | `LightningTrail` | 12·DXT5 | 精确 | 128×128 |
| 33 | `ui_menu/Roman_I.png.meta` | `Roman I` | 12·DXT5 | 改名 | 128×128 |
| 34 | `ui_menu/Roman_II.png.meta` | `Roman II` | 12·DXT5 | 改名 | 128×128 |
| 35 | `ui_menu/Roman_III.png.meta` | `Roman III` | 12·DXT5 | 改名 | 128×128 |
| 36 | `ui_menu/Roman_IV.png.meta` | `Roman IV` | 12·DXT5 | 改名 | 128×128 |
| 37 | `ui_menu/Roman_V.png.meta` | `Roman V` | 12·DXT5 | 改名 | 128×128 |
| 38 | `ui_menu/Roman_VI.png.meta` | `Roman VI` | 12·DXT5 | 改名 | 128×128 |
| 39 | `ui_menu/Shine_trail.png.meta` | `Shine trail` | 12·DXT5 | 改名 | 128×128 |
| 40 | `ui_menu/UI_Dirt_And_Noise_skratches.png.meta` | `UI Dirt And Noise skratches` | 12·DXT5 | 改名 | 1024×1024 |

### C · 原版 DXT5Crunched → `Compressed` + 显式 DXT5 + `crunchedCompression: 1` —— 64 张

改后：Default `(textureFormat 12, textureCompression 1, crunchedCompression 1, overridden 0)` · Standalone `(12, 1, 1, overridden 1)`

| # | 落盘文件（`Resources/Art/…`） | 原版名 | 原版 fmt | 匹配级 | 尺寸 |
|---|---|---|---|---|---|
| 1 | `ui_menu/40K_shop_offer_booster_Space_Wolves.png.meta` | `40K_shop_offer_booster_Space Wolves` | 29·DXT5Crunched | 改名 | 1024×1024 |
| 2 | `ui_menu/Container_Image_Practice_1x1.png.meta` | `Container Image Practice 1x1` | 29·DXT5Crunched | 改名 | 1024×1024 |
| 3 | `ui_menu/UI_Deck_Master_Zacharial.png.meta` | `UI_Deck_Master Zacharial` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 4 | `ui_menu/UI_Deck_Warlord_Abaddon.png.meta` | `UI_Deck_Warlord_Abaddon` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 5 | `ui_menu/UI_Deck_Warlord_Abaddon_AA_HB.png.meta` | `UI_Deck_Warlord_Abaddon_AA_HB` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 6 | `ui_menu/UI_Deck_Warlord_Acolyte_Iconward.png.meta` | `UI_Deck_Warlord_Acolyte Iconward` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 7 | `ui_menu/UI_Deck_Warlord_Anvirr_Keltoc.png.meta` | `UI_Deck_Warlord_Anvirr Keltoc` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 8 | `ui_menu/UI_Deck_Warlord_Asmodai.png.meta` | `UI_Deck_Warlord_Asmodai` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 9 | `ui_menu/UI_Deck_Warlord_AunVa.png.meta` | `UI_Deck_Warlord_AunVa` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 10 | `ui_menu/UI_Deck_Warlord_Azrael.png.meta` | `UI_Deck_Warlord_Azrael` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 11 | `ui_menu/UI_Deck_Warlord_AzraelAA.png.meta` | `UI_Deck_Warlord_AzraelAA` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 12 | `ui_menu/UI_Deck_Warlord_Beastboss_Morgrim.png.meta` | `UI_Deck_Warlord_Beastboss Morgrim` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 13 | `ui_menu/UI_Deck_Warlord_Belial.png.meta` | `UI_Deck_Warlord_Belial` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 14 | `ui_menu/UI_Deck_Warlord_Boss_Zagstruk.png.meta` | `UI_Deck_Warlord_Boss Zagstruk` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 15 | `ui_menu/UI_Deck_Warlord_Celestian_Sacresant_Aveline.png.meta` | `UI_Deck_Warlord_Celestian Sacresant Aveline` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 16 | `ui_menu/UI_Deck_Warlord_Chaplain_Letharius.png.meta` | `UI_Deck_Warlord_Chaplain Letharius` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 17 | `ui_menu/UI_Deck_Warlord_Commander_OMaisos.png.meta` | `UI_Deck_Warlord_Commander OMaisos` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 18 | `ui_menu/UI_Deck_Warlord_Commissar_Deckler.png.meta` | `UI_Deck_Warlord_Commissar Deckler` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 19 | `ui_menu/UI_Deck_Warlord_Daemon_Prince.png.meta` | `UI_Deck_Warlord_Daemon Prince` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 20 | `ui_menu/UI_Deck_Warlord_Eliac_Zephyrblade.png.meta` | `UI_Deck_Warlord_Eliac Zephyrblade` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 21 | `ui_menu/UI_Deck_Warlord_Erika_Luminas.png.meta` | `UI_Deck_Warlord_Erika Luminas` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 22 | `ui_menu/UI_Deck_Warlord_Ghazghkull_AA_HB.png.meta` | `UI_Deck_Warlord_Ghazghkull_AA_HB` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 23 | `ui_menu/UI_Deck_Warlord_Ghazghkull_Thraka.png.meta` | `UI_Deck_Warlord_Ghazghkull Thraka` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 24 | `ui_menu/UI_Deck_Warlord_Gordrang.png.meta` | `UI_Deck_Warlord_Gordrang` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 25 | `ui_menu/UI_Deck_Warlord_Grukk_Face-Rippa.png.meta` | `UI_Deck_Warlord_Grukk Face-Rippa` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 26 | `ui_menu/UI_Deck_Warlord_Guilliman.png.meta` | `UI_Deck_Warlord_Guilliman` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 27 | `ui_menu/UI_Deck_Warlord_Haarken_Worldclaimer.png.meta` | `UI_Deck_Warlord_Haarken Worldclaimer` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 28 | `ui_menu/UI_Deck_Warlord_Hektor_Thenmann.png.meta` | `UI_Deck_Warlord_Hektor Thenmann` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 29 | `ui_menu/UI_Deck_Warlord_Imotekh_AA_HB.png.meta` | `UI_Deck_Warlord_Imotekh_AA_HB` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 30 | `ui_menu/UI_Deck_Warlord_Imotekh_the_Stormlord.png.meta` | `UI_Deck_Warlord_Imotekh the Stormlord` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 31 | `ui_menu/UI_Deck_Warlord_Jain_Zar.png.meta` | `UI_Deck_Warlord_Jain Zar` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 32 | `ui_menu/UI_Deck_Warlord_Junith_Eruita.png.meta` | `UI_Deck_Warlord_Junith Eruita` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 33 | `ui_menu/UI_Deck_Warlord_Leontus.png.meta` | `UI_Deck_Warlord_Leontus` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 34 | `ui_menu/UI_Deck_Warlord_Lhaska_Szenari.png.meta` | `UI_Deck_Warlord_Lhaska Szenari` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 35 | `ui_menu/UI_Deck_Warlord_Lhykhis.png.meta` | `UI_Deck_Warlord_Lhykhis` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 36 | `ui_menu/UI_Deck_Warlord_Logan_AA_HB.png.meta` | `UI_Deck_Warlord_Logan_AA_HB` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 37 | `ui_menu/UI_Deck_Warlord_Logan_Grimnar.png.meta` | `UI_Deck_Warlord_Logan Grimnar` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 38 | `ui_menu/UI_Deck_Warlord_Lord_Exultant.png.meta` | `UI_Deck_Warlord_Lord Exultant` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 39 | `ui_menu/UI_Deck_Warlord_Lucius.png.meta` | `UI_Deck_Warlord_Lucius` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 40 | `ui_menu/UI_Deck_Warlord_Magus.png.meta` | `UI_Deck_Warlord_Magus` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 41 | `ui_menu/UI_Deck_Warlord_Marneus_Calgar.png.meta` | `UI_Deck_Warlord_Marneus Calgar` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 42 | `ui_menu/UI_Deck_Warlord_Medreyal_Ghaelyn.png.meta` | `UI_Deck_Warlord_Medreyal Ghaelyn` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 43 | `ui_menu/UI_Deck_Warlord_Morvenn_Vahl.png.meta` | `UI_Deck_Warlord_Morvenn Vahl` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 44 | `ui_menu/UI_Deck_Warlord_Mozrog_Skragbad.png.meta` | `UI_Deck_Warlord_Mozrog Skragbad` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 45 | `ui_menu/UI_Deck_Warlord_Nemesor_Zahndrekh.png.meta` | `UI_Deck_Warlord_Nemesor Zahndrekh` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 46 | `ui_menu/UI_Deck_Warlord_Neurothrope.png.meta` | `UI_Deck_Warlord_Neurothrope` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 47 | `ui_menu/UI_Deck_Warlord_Njal_Stormcaller.png.meta` | `UI_Deck_Warlord_Njal Stormcaller` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 48 | `ui_menu/UI_Deck_Warlord_Orikan_the_Diviner.png.meta` | `UI_Deck_Warlord_Orikan the Diviner` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 49 | `ui_menu/UI_Deck_Warlord_Ragnar_Blackmane.png.meta` | `UI_Deck_Warlord_Ragnar Blackmane` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 50 | `ui_menu/UI_Deck_Warlord_Ramatekh.png.meta` | `UI_Deck_Warlord_Ramatekh` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 51 | `ui_menu/UI_Deck_Warlord_Saffa_Rhiannor.png.meta` | `UI_Deck_Warlord_Saffa Rhiannor` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 52 | `ui_menu/UI_Deck_Warlord_Shadowsun.png.meta` | `UI_Deck_Warlord_Shadowsun` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 53 | `ui_menu/UI_Deck_Warlord_Swarmlord.png.meta` | `UI_Deck_Warlord_Swarmlord` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 54 | `ui_menu/UI_Deck_Warlord_Sylar_Hexcorn.png.meta` | `UI_Deck_Warlord_Sylar Hexcorn` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 55 | `ui_menu/UI_Deck_Warlord_Tempestor_Prime.png.meta` | `UI_Deck_Warlord_Tempestor Prime` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 56 | `ui_menu/UI_Deck_Warlord_Tervigon.png.meta` | `UI_Deck_Warlord_Tervigon` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 57 | `ui_menu/UI_Deck_Warlord_Titus.png.meta` | `UI_Deck_Warlord_Titus` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 58 | `ui_menu/UI_Deck_Warlord_Titus_AA_HB.png.meta` | `UI_Deck_Warlord_Titus_AA_HB` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 59 | `ui_menu/UI_Deck_Warlord_Uriel_Ventris.png.meta` | `UI_Deck_Warlord_Uriel Ventris` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 60 | `ui_menu/UI_Deck_Warlord_Ursula_AA_HB.png.meta` | `UI_Deck_Warlord_Ursula_AA_HB` | 29·DXT5Crunched | 精确 | 2048×2048 |
| 61 | `ui_menu/UI_Deck_Warlord_Ursula_Creed.png.meta` | `UI_Deck_Warlord_Ursula Creed` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 62 | `ui_menu/UI_Deck_Warlord_Valius_Paxor.png.meta` | `UI_Deck_Warlord_Valius Paxor` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 63 | `ui_menu/UI_Deck_Warlord_Varro_Tigurius.png.meta` | `UI_Deck_Warlord_Varro Tigurius` | 29·DXT5Crunched | 改名 | 2048×2048 |
| 64 | `ui_menu/UI_Deck_Warlord_Winged_Tyranid_Prime.png.meta` | `UI_Deck_Warlord_Winged Tyranid Prime` | 29·DXT5Crunched | 改名 | 2048×2048 |

### D1 · 原版 DXT1 → `Compressed` + 显式 DXT1 —— 1 张

改后：Default `(textureFormat 10, textureCompression 1, crunchedCompression 0, overridden 0)` · Standalone `(10, 1, 0, overridden 1)`

| # | 落盘文件（`Resources/Art/…`） | 原版名 | 原版 fmt | 匹配级 | 尺寸 |
|---|---|---|---|---|---|
| 1 | `ui/White_Square.png.meta` | `White Square` | 10·DXT1 | 改名 | 8×8 |

### D2 · 原版 DXT1Crunched → `Compressed` + 显式 DXT1 + `crunchedCompression: 1` —— 6 张

改后：Default `(textureFormat 10, textureCompression 1, crunchedCompression 1, overridden 0)` · Standalone `(10, 1, 1, overridden 1)`

| # | 落盘文件（`Resources/Art/…`） | 原版名 | 原版 fmt | 匹配级 | 尺寸 |
|---|---|---|---|---|---|
| 1 | `ui_menu/40k_main_GameMode_Skirmish.png.meta` | `40k_main_GameMode_Skirmish` | 28·DXT1Crunched | 精确 | 1024×1024 |
| 2 | `ui_menu/Container_Image_Draft.png.meta` | `Container Image Draft` | 28·DXT1Crunched | 改名 | 1024×1024 |
| 3 | `ui_menu/Container_Image_Ranked.png.meta` | `Container Image Ranked` | 28·DXT1Crunched | 改名 | 1024×1024 |
| 4 | `ui_menu/Container_Image_Tutorial.png.meta` | `Container Image Tutorial` | 28·DXT1Crunched | 改名 | 1024×1024 |
| 5 | `ui_menu/Space_Wolves_Container_1x2.png.meta` | `Space Wolves Container 1x2` | 28·DXT1Crunched | 改名 | 1024×1024 |
| 6 | `ui_menu/UM_Guilliman_Container_1x2.png.meta` | `UM Guilliman Container 1x2` | 28·DXT1Crunched | 改名 | 1024×1024 |

---

## 4 · 表外 / ❓（保持现值未改）

### 4·1 `Noise_Combined.png`（1 张）

原版 `Noise Combined` = **RGBA32（fmt 4）**，现值 `DefaultTexturePlatform.textureCompression: 2`。**按任务指令未动**（"W1 已处理"：W1 已把该名字加进 `ArtBaker.CompTruths`，下次烘焙会写 `Uncompressed`）。⛔ 这里**没有**手工改，避免与 W1 的烘焙路径打架。

### 4·2 原版 BC7 的 81 张（不改）

`m_TextureFormat = 25`(BC7) ⇒ 我们现值 `CompressedHQ` 就是 BC7，**同档**。名字这批以 `Attack_type_button_*`、`FX_Square_UI_SDF` 等为主，逐张见 `_tmp_view/b141_w40/targets_all.json` 之外的匹配表（本报告不重复列）。

### 4·3 无法按名对回原版 `Texture2D` 的 442 张 ❓

这些多半是**图集内切出来的 Sprite**（原版里只有 `Sprite` 资产、没有同名 `Texture2D`），所以 `orig_textures.json` 里没有它们的 `m_TextureFormat` 真值。**全部保持现值未改。**已试过三种收紧匹配（剥 `_N` 后缀 / 忽略大小写 / 剥状态后缀），命中 0（除 `40K_shop_offer_bg_Sororitas_0` 一条属 15-08/15-04）。

> **下一步要解这 442 张的判据**：原版 `Sprite/*.json` 里 `m_RD.texture` 是 0（packed），真指向在 `m_SpriteAtlas` PPtr ⇒ 要按 bundle 解 `SpriteAtlas` → 它的 `m_PackedSprites`/纹理，再读那张图集的 `m_TextureFormat`。这不是「猜」能做的，本轮未做（白名单也不含重跑 UnityPy）。

完整名单（442，按字母序，直接取自现读文件名）：

```
  ui/40K_button, ui/40K_display, ui/40K_melee_glow, ui/40K_ranged_glow
  ui/40K_replay_bt_next, ui/40K_replay_bt_next_ushed, ui/40K_replay_bt_pause, ui/40K_replay_bt_pause_pushed
  ui/40K_replay_bt_play, ui/40K_replay_bt_play_pushed, ui/40K_replay_bt_restart, ui/40K_replay_bt_restart_pushed
  ui/40k_Battle_Display_Faith, ui/40k_Combat_Icon_Cross, ui/40k_DeckHolder_light_green, ui/40k_DeckHolder_light_red
  ui/40k_UI_bt_battlelog, ui/40k_UI_bt_battlelog_hover, ui/40k_UI_bt_battlelog_pressed, ui/40k_UI_bt_center_camera
  ui/40k_UI_bt_eye, ui/40k_UI_bt_play, ui/40k_UI_bt_play_hover, ui/40k_UI_bt_play_pressed
  ui/40k_UI_bt_voicelines, ui/40k_UnitChat_Background_Bottom, ui/40k_UnitChat_Background_Left, ui/40k_UnitChat_Background_Right
  ui/40k_UnitChat_Background_Top, ui/40k_battle_Win_Skull, ui/40k_battle_Win_Skull2, ui/40k_battle_Win_Skull3
  ui/40k_battle_energy_empty, ui/40k_battle_energy_full, ui/40k_battle_icon_environmental, ui/40k_battlelog_display_enemy
  ui/40k_battlelog_display_neutral, ui/40k_battlelog_display_player, ui/40k_battlelog_frame_Bottom, ui/40k_battlelog_frame_Left
  ui/40k_battlelog_frame_Right, ui/40k_battlelog_frame_TOP, ui/40k_bt_close, ui/40k_bt_underbutton
  ui/40k_dsplay_overtime, ui/40k_general_bt_yellow, ui/40k_icon_overtime, ui/40k_main_bt_nametag
  ui/40k_voicelines_bt_L, ui/40k_voicelines_bt_L_blocked, ui/40k_voicelines_bt_L_hover, ui/40k_voicelines_bt_L_pressed
  ui/40k_voicelines_bt_R, ui/40k_voicelines_bt_R_blocked, ui/40k_voicelines_bt_R_hover, ui/40k_voicelines_bt_R_pressed
  ui/40k_voicelines_radio, ui/Atack_Icon_BW, ui/Attack_type_Generic_Foreground, ui/Attack_type_button_Melee
  ui/Attack_type_button_Ranged, ui/Attack_type_button_highlight, ui/Base3d_Trait_Background, ui/Minion_Death_Icon
  ui/Player_Profile_Border, ui/Player_Profile_Border_Gold, ui/UI_Button_End_Turn_Hover_wide, ui/UI_Button_End_Turn_Normal_wide
  ui/UI_Button_End_Turn_Pressed_wide, ui/UI_Button_Mulligan, ui/UI_Button_Mulligan_Pressed, ui/UI_Button_Mulligan_hover
  ui/UI_Button_Round_background, ui/UI_Deck_Background, ui/UI_Energy_Eldar, ui/UI_Energy_Holder_big
  ui/UI_Gem_Eldar, ui/UI_PlayerFrame_TitleBackground, ui/UI_Player_Frame, ui/UI_Quest_Points
  ui/UI_Quest_Points_Joint, ui/UI_Settings_Icon, ui/Volume_bar_active, ui/Volume_bar_inactive
  ui/Volume_button, ui/WF_UI_Trophy_Gold, ui/flavourbg_astramilitarum, ui/flavourbg_blacklegion
  ui/flavourbg_darkangels, ui/flavourbg_emperorschildren, ui/flavourbg_genestealers, ui/flavourbg_goff
  ui/flavourbg_leviathan, ui/flavourbg_saimhann, ui/flavourbg_sautekh, ui/flavourbg_sororitas
  ui/flavourbg_spacewolves, ui/flavourbg_tauempire, ui/flavourbg_ultramarines, ui_deck/1_40k_cardframe_rarity_common
  ui_deck/3_40k_cardframe_rarity_epic, ui_deck/40K_bt_deck, ui_deck/40K_dropdown_arrow_closed, ui_deck/40K_dropdown_arrow_opened
  ui_deck/40K_dropdown_bg, ui_deck/40K_dropdown_field_closed, ui_deck/40K_dropdown_field_opened, ui_deck/40K_dropdown_item
  ui_deck/40K_dropdown_item_hover, ui_deck/40K_dropdown_item_press, ui_deck/40K_dropdown_item_selected, ui_deck/40K_main_deck_card_counter
  ui_deck/40K_tab_button, ui_deck/40K_tab_button_overwindow, ui_deck/40K_toggle_off, ui_deck/40K_toggle_on
  ui_deck/40_main_bt_toggle_off, ui_deck/40_main_bt_toggle_on, ui_deck/40k_CardAmount_bar_bg, ui_deck/40k_CardAmount_bar_fill
  ui_deck/40k_DeckSelection_icon_FactionAstraMilitarum, ui_deck/40k_DeckSelection_icon_FactionEmperorsChildren, ui_deck/40k_DeckSelection_icon_FactionLeviathan, ui_deck/40k_DeckSelection_icon_FactionOrks
  ui_deck/40k_DeckSelection_icon_FactionSaimHann, ui_deck/40k_DeckSelection_icon_FactionSautekh, ui_deck/40k_DeckSelection_icon_FactionSororitas, ui_deck/40k_DeckSelection_icon_FactionTauEmpire
  ui_deck/40k_DeckSelection_icon_Genestealers, ui_deck/40k_DeckSelection_icon_SpaceWolves, ui_deck/40k_Smooth_shadow_background, ui_deck/40k_UI_bt_deck
  ui_deck/40k_UI_bt_deck_change, ui_deck/40k_UI_bt_deck_hover, ui_deck/40k_UI_bt_deck_pressed, ui_deck/40k_UI_icon_deck
  ui_deck/40k_bt_icon_search, ui_deck/40k_bt_outline, ui_deck/40k_collection_bt_avatars, ui_deck/40k_collection_bt_cards
  ui_deck/40k_collection_bt_cosmetics, ui_deck/40k_collection_bt_decks, ui_deck/40k_collection_bt_style, ui_deck/40k_deck_cardlist_bg
  ui_deck/40k_deck_cardlist_bg_rarityColorGradient, ui_deck/40k_deck_cardlist_border, ui_deck/40k_deck_cardlist_doted_bg, ui_deck/40k_deck_cardlist_outline
  ui_deck/40k_general_bt_yellow_back, ui_deck/40k_general_bt_yellow_buy, ui_deck/40k_general_bt_yellow_close, ui_deck/40k_general_bt_yellow_confirm
  ui_deck/40k_general_bt_yellow_delete, ui_deck/40k_general_bt_yellow_duplicate, ui_deck/40k_general_bt_yellow_edit, ui_deck/40k_general_bt_yellow_share
  ui_deck/40k_general_icon_card_amount, ui_deck/40k_general_wildcard_common, ui_deck/40k_general_wildcard_common_small, ui_deck/40k_general_wildcard_epic
  ui_deck/40k_general_wildcard_epic_small, ui_deck/40k_general_wildcard_legendary, ui_deck/40k_general_wildcard_legendary_small, ui_deck/40k_general_wildcard_rare
  ui_deck/40k_general_wildcard_rare_small, ui_deck/40k_icon_search, ui_deck/40k_main_bt_selected_BW, ui_deck/40k_main_line
  ui_deck/40k_main_tab_background, ui_deck/40k_main_tab_shadow, ui_deck/40k_menu_bt, ui_deck/40k_menu_scroll_bar_bg
  ui_deck/40k_menu_scroll_bar_fill, ui_deck/40k_menu_search_icon_warlord, ui_deck/40k_popup, ui_deck/40k_topmarquee_currency_display_BW
  ui_deck/4_40k_cardframe_rarity_legendary, ui_deck/5_40k_cardframe_rarity_special, ui_deck/Card_Frame_Cost_Icon, ui_deck/UI_Card_name_background_normal_BW
  ui_deck/UI_Deck_Information_Back, ui_deck/UI_Deck_Information_Tab, ui_deck/UI_Deck_Selection_Back, ui_deck/UI_Deck_Selection_Back_simple
  ui_deck/pedestal_icon_armor, ui_menu/40K_ArmyTrack_bar, ui_menu/40K_ArmyTrack_bar_fill, ui_menu/40K_ArmyTrack_bg
  ui_menu/40K_ArmyTrack_chosen_faction, ui_menu/40K_Cards_button_draft_menu, ui_menu/40K_Chat_icon_Alliance_v2, ui_menu/40K_Chat_icon_Global
  ui_menu/40K_Icon_Discount_Gold, ui_menu/40K_Info_button_draft_menu, ui_menu/40K_Profile_icon_Trophies, ui_menu/40K_Profile_icon_avatar
  ui_menu/40K_Profile_icon_battlelog, ui_menu/40K_Profile_icon_profile, ui_menu/40K_Profile_icon_title, ui_menu/40K_bt_View_Friend
  ui_menu/40K_bt_addFriend, ui_menu/40K_bt_challenge1, ui_menu/40K_bt_challenge2, ui_menu/40K_bt_deleteFriend
  ui_menu/40K_button, ui_menu/40K_button_hover, ui_menu/40K_button_pressed, ui_menu/40K_button_square
  ui_menu/40K_button_square_hover, ui_menu/40K_button_square_pressed, ui_menu/40K_dropdown_field_closed, ui_menu/40K_dropdown_field_opened
  ui_menu/40K_general_icon_Expansion_Pass_XP, ui_menu/40K_general_icon_Forge_points, ui_menu/40K_general_icon_Forge_points_AstraMilitarum, ui_menu/40K_general_icon_Forge_points_BlackLegion
  ui_menu/40K_general_icon_Forge_points_Dark_Angels, ui_menu/40K_general_icon_Forge_points_Emperors_Children, ui_menu/40K_general_icon_Forge_points_Genestealers, ui_menu/40K_general_icon_Forge_points_Goff
  ui_menu/40K_general_icon_Forge_points_Leviathan, ui_menu/40K_general_icon_Forge_points_SaimHann, ui_menu/40K_general_icon_Forge_points_Sautekh, ui_menu/40K_general_icon_Forge_points_Sororitas
  ui_menu/40K_general_icon_Forge_points_SpaceWolves, ui_menu/40K_general_icon_Forge_points_TauEmpire, ui_menu/40K_general_icon_Forge_points_Ultramarines, ui_menu/40K_icon_duel
  ui_menu/40K_icon_feedback, ui_menu/40K_icon_menu_chat, ui_menu/40K_icon_searching_cog, ui_menu/40K_icon_searching_skull
  ui_menu/40K_icon_status_offline, ui_menu/40K_icon_status_online, ui_menu/40K_main_deck_card_counter, ui_menu/40K_main_rank_display
  ui_menu/40K_menu_loading, ui_menu/40K_missions_display_Daily_horizontal, ui_menu/40K_missions_display_Daily_vertical, ui_menu/40K_missions_display_Weekly
  ui_menu/40K_missions_icon_Daily_skulls, ui_menu/40K_missions_icon_login_bonus, ui_menu/40K_notification, ui_menu/40K_notification_number
  ui_menu/40K_profile_ForgeLevel_bg, ui_menu/40K_profile_campaign_bg_AstraMilitarum, ui_menu/40K_profile_campaign_bg_Black_Legion, ui_menu/40K_profile_campaign_bg_DarkAngels
  ui_menu/40K_profile_campaign_bg_Emperors_Children, ui_menu/40K_profile_campaign_bg_GenestealerCults, ui_menu/40K_profile_campaign_bg_Leviathan, ui_menu/40K_profile_campaign_bg_Orks
  ui_menu/40K_profile_campaign_bg_SW, ui_menu/40K_profile_campaign_bg_SaimHann, ui_menu/40K_profile_campaign_bg_Sautekh, ui_menu/40K_profile_campaign_bg_Sororitas
  ui_menu/40K_profile_campaign_bg_Tau_Empire, ui_menu/40K_profile_campaign_bg_UM, ui_menu/40K_rewards_bt_forge, ui_menu/40K_rewards_bt_missions
  ui_menu/40K_settings_button, ui_menu/40K_settings_button_account, ui_menu/40K_settings_button_general, ui_menu/40K_settings_button_graphics
  ui_menu/40K_settings_button_hover, ui_menu/40K_settings_button_pressed, ui_menu/40K_settings_button_quality, ui_menu/40K_settings_button_selected
  ui_menu/40K_settings_button_support, ui_menu/40K_settings_icon_checkmark, ui_menu/40K_shop_bt_boosters, ui_menu/40K_shop_bt_campaigns
  ui_menu/40K_shop_bt_cards, ui_menu/40K_shop_bt_cards_vip, ui_menu/40K_shop_bt_cosmetics, ui_menu/40K_shop_offer_bg_Sororitas_0
  ui_menu/40k_Achievements_icon_medal1, ui_menu/40k_Achievements_icon_medal2, ui_menu/40k_Achievements_icon_medal3, ui_menu/40k_Achievements_icon_medal4
  ui_menu/40k_Achievements_icon_medal5, ui_menu/40k_Achievements_icon_seal_points, ui_menu/40k_ArmyTrack_milestone_off, ui_menu/40k_ArmyTrack_milestone_on
  ui_menu/40k_Cross_icon_cross_big_Banned_card, ui_menu/40k_DraftMode_icon_cross, ui_menu/40k_OfferBadge, ui_menu/40k_Profile_display_title
  ui_menu/40k_Rewards_icon__, ui_menu/40k_Rewards_icon_base_card, ui_menu/40k_Rewards_icon_base_card_common, ui_menu/40k_Rewards_icon_base_card_epic
  ui_menu/40k_Rewards_icon_base_card_legendary, ui_menu/40k_Rewards_icon_base_card_rare, ui_menu/40k_Rewards_icon_title_card, ui_menu/40k_Separator_Fade_Sides_Horizontal
  ui_menu/40k_Separator_Fade_Sides_Vertical, ui_menu/40k_Smooth_shadow_background, ui_menu/40k_UI_Banner_BW, ui_menu/40k_UI_Chat_send
  ui_menu/40k_UI_bt_back, ui_menu/40k_UI_bt_back_hover, ui_menu/40k_UI_bt_back_hover_back, ui_menu/40k_UI_bt_back_pressed
  ui_menu/40k_UI_bt_deck_change_hover, ui_menu/40k_UI_bt_eye_hover, ui_menu/40k_UI_bt_eye_pressed, ui_menu/40k_UI_bt_voicelines_hover
  ui_menu/40k_UI_bt_voicelines_pressed, ui_menu/40k_alliances_bt_friends_v2, ui_menu/40k_alliances_icon_chat, ui_menu/40k_bt_close_hover
  ui_menu/40k_bt_close_pressed, ui_menu/40k_bt_eye, ui_menu/40k_bt_eye_hover, ui_menu/40k_campaign_Expansion_Pass
  ui_menu/40k_campaign_Premium-icon, ui_menu/40k_campaign_bar_bg, ui_menu/40k_campaign_bar_end, ui_menu/40k_campaign_bar_fill
  ui_menu/40k_campaign_bar_fill_color, ui_menu/40k_campaign_bar_outline, ui_menu/40k_dropdown_bg, ui_menu/40k_general_bt_arrow
  ui_menu/40k_general_bt_yellow_delete, ui_menu/40k_general_bt_yellow_hover, ui_menu/40k_general_bt_yellow_pin_replay, ui_menu/40k_general_bt_yellow_pressed
  ui_menu/40k_general_bt_yellow_replay, ui_menu/40k_general_bt_yellow_seedeck, ui_menu/40k_general_bt_yellow_share_in_chat, ui_menu/40k_general_icon_card_amount
  ui_menu/40k_general_popup_simple_greyscale, ui_menu/40k_general_popup_simple_red, ui_menu/40k_generial_bar_empty, ui_menu/40k_generial_bar_fill
  ui_menu/40k_generic_bt_info, ui_menu/40k_icon_DailyReward, ui_menu/40k_icon_DailyReward_Premium, ui_menu/40k_main_bt_alliances
  ui_menu/40k_main_bt_campaign, ui_menu/40k_main_bt_collection, ui_menu/40k_main_bt_event, ui_menu/40k_main_bt_friends
  ui_menu/40k_main_bt_play, ui_menu/40k_main_bt_rewards, ui_menu/40k_main_bt_shop, ui_menu/40k_main_collection_icon
  ui_menu/40k_main_line_purple, ui_menu/40k_main_player_frame, ui_menu/40k_menu_bt, ui_menu/40k_menu_bt__general_outline
  ui_menu/40k_menu_bt_general_bg, ui_menu/40k_menu_bt_pressed, ui_menu/40k_menu_scroll_bar_bg, ui_menu/40k_menu_search_icon_stratagem
  ui_menu/40k_menu_search_icon_troop, ui_menu/40k_missions_milestone_off, ui_menu/40k_missions_milestone_on, ui_menu/40k_popup
  ui_menu/40k_profile_icon_copy, ui_menu/40k_ranking_icon_trophy_Plus, ui_menu/40k_replace_display, ui_menu/40k_rewards_forge_decoration_1_candle
  ui_menu/40k_rewards_forge_decoration_1_candle_light, ui_menu/40k_rewards_forge_decoration_2, ui_menu/40k_rewards_forge_decoration_Column_down, ui_menu/40k_rewards_forge_decoration_Column_mid
  ui_menu/40k_rewards_forge_decoration_Column_top, ui_menu/40k_shop_bt_gold, ui_menu/40k_shop_bt_ticket, ui_menu/40k_square_border
  ui_menu/40k_topmarquee_currency_display_BW, ui_menu/Button_External_Link, ui_menu/Cell_Shaded_Alternative_Art_Style, ui_menu/Chat_background
  ui_menu/Chat_text_background, ui_menu/Closed-Chat_background, ui_menu/Discord-Logo-Color, ui_menu/Feedback_Scoring_Button
  ui_menu/GenericRoundedSquare, ui_menu/Glow_UI_sharp_W40K, ui_menu/Highlight_Rounded_Square, ui_menu/Instagram_icon
  ui_menu/Menu_Icon_Gallons_1, ui_menu/Menu_Icon_Gallons_2, ui_menu/Menu_Icon_Gallons_3, ui_menu/Menu_Icon_Galon
  ui_menu/MiniBar_01, ui_menu/Purity_Seal_02, ui_menu/Rank_Skull, ui_menu/Rank_Skull_0
  ui_menu/Rank_Skull_Empty, ui_menu/Smooth_background_lateral, ui_menu/Steam_icon_logo.svg, ui_menu/Tutorial_Background
  ui_menu/Twitter_Social_Icon_Rounded_Square_Color, ui_menu/UI_Army_Selection_Back, ui_menu/UI_Army_Selection_Back_Hover, ui_menu/UI_Army_Selection_Back_Pressed
  ui_menu/UI_Army_Selection_Back_Progression, ui_menu/UI_Army_Selection_Featured, ui_menu/UI_Background_faction_buttons, ui_menu/UI_Button_Menu_Back
  ui_menu/UI_Button_Menu_Back_Hover, ui_menu/UI_Button_Menu_Back_Pressed, ui_menu/UI_Button_Mulligan, ui_menu/UI_Button_Mulligan_Pressed
  ui_menu/UI_Button_Mulligan_hover, ui_menu/UI_Button_Organe_Square_Hover, ui_menu/UI_Button_Organe_Square_Normal, ui_menu/UI_Button_Round_background
  ui_menu/UI_Deck_Information_submenu_Back, ui_menu/UI_Deck_Information_submenu_Back_opaque, ui_menu/UI_Deck_button_click, ui_menu/UI_Draft_event_result_bar_holder
  ui_menu/UI_HIghlight_Internal, ui_menu/UI_Login_Tracker, ui_menu/UI_Main_Upper_bar, ui_menu/UI_icon_shield
  ui_menu/VS_icon, ui_menu/Victories_Background_V2, ui_menu/Victory_round_back_with_light, ui_menu/Victory_round_v2
  ui_menu/Victory_skull, ui_menu/WF_9Sliced, ui_menu/WF_Campaign_Info_Background, ui_menu/WF_Campaign_Levelspot
  ui_menu/WF_Lock_Icon_Simple, ui_menu/WF_Login_CornerBanner, ui_menu/WF_Special_offer_Bck_tier1, ui_menu/WF_Special_offer_Bck_tier2
  ui_menu/WF_Special_offer_Bck_tier3, ui_menu/WF_Special_offer_Value, ui_menu/WF_UI_Generic_FancyNotification_Background, ui_menu/WF_UI_Ranked_Background_Bronze
  ui_menu/WF_UI_Ranked_Background_Gold, ui_menu/WF_UI_Ranked_Background_Silver, ui_menu/WF_icon_clock, ui_menu/WarpForgeLogo_Style
  ui_menu/YouTube_full-color_icon_(2017).svg, ui_menu/fb-icon
```

---

## 5 · 验证（二进制读回，全部现读）

| 项 | 结果 |
|---|---|
| 改动文件数 | **137**（`targets_all.json` 逐条） |
| 行数守恒 | **137/137 不变**（改前/改后 `split(b'\n')` 等长） |
| 行尾 | **137/137 仍是纯 LF**（`CRLF = 0` · 裸 `CR = 0`） |
| 只动了 4 个键 | 逐行 diff：**所有差异行都落在 `platformSettings` 内、且键名 ∈ {textureFormat, textureCompression, crunchedCompression, overridden}**；无一处意外差异 |
| 目标值现读 | **137/137 的两个块四键 = 计划值**（脚本 `verify.py`，BAD 0） |
| YAML 可解析 | 抽样 `yaml.safe_load` 通过（3/3） |
| 自检 | **零条**（纯资产任务，无 `.cs` 改动 ⇒ 不跑 typecheck / 不跑宿主） |

备份：`_tmp_view/b141_w40/backup/`（137 份改前原件，文件名把 `/` 换成 `__`）。回滚脚本思路：直接 `copyfile` 回去即可。

---

## 6 · 可持续性：必须同步 `ArtBaker.CompTruths`（否则下次烘焙被打回）

**为什么**：`ArtBaker.ApplyImportSettings()`（`ArtBaker.cs:433-535`）对 `Resources/Art` 下**每一张** Texture2D 无条件写 `ti.textureCompression = isSdf ? Uncompressed : CompressedHQ`，随后只对 `CompTruths` 表内（现 8 个名字）调 `ApplyCompressionTruth`。本轮的 136 个唯一原版名**不在表内** ⇒ 下次 `BakeAll` / `BakeFromCLI` 会把它们**静默写回 BC7**。

⇒ **需要 W1 的写手**（`ArtBaker.cs` 本批归 W1）做两件事：
1. 把下表并进 `CompTruths`（`OriginalCompression` 已经会按「原版名 / 下划线形态」查，无需再改查表逻辑）。
2. **`ArtCompressionTruth` 目前只有 `Compression` + `ExplicitFormat`，没有 Crunch** —— 70 张（DXT5Crunched 64 + DXT1Crunched 6）需要新增 `Crunched` 字段并在 `ApplyCompressionTruth` 里写 `ps.crunchedCompression`。

真值表（按目标档分组，名字 = 原版名；`#` 是文件实例数）：

```csharp
// textureFormat=-1, textureCompression=0, crunchedCompression=0  (25 个名字)
    { "2_40k_cardframe_rarity_rare", ... },
    { "40K_ArmyTrack_bar_position", ... },
    { "40k_DeckSelection_icon_FactionBlackLegion", ... },
    { "40k_DeckSelection_icon_FactionDarkAngels", ... },
    { "40k_DeckSelection_icon_FactionUM", ... },
    { "40k_Generic Smooth line", ... },
    { "40k_Rewards_icon_base card SDF", ... },
    { "40k_battle_Win Skull SDF", ... },
    { "40k_popup_texture", ... },
    { "40k_shop_popup_info_bg", ... },
    { "40k_topmarquee_currency_blackstone", ... },
    { "40k_topmarquee_currency_crystal", ... },
    { "40k_topmarquee_currency_energy", ... },
    { "40k_topmarquee_currency_gold", ... },
    { "40k_topmarquee_currency_ticket", ... },
    { "40k_voicelines_radio_wave equalizer", ... },
    { "Background", ... },
    { "Card Text smooth background", ... },
    { "Crate Border Highlight", ... },
    { "Glow UI W40K", ... },
    { "InputFieldBackground", ... },
    { "OctagonUI Border SDF 2", ... },
    { "Smooth background square", ... },
    { "Tutorial Highlight", ... },
    { "WF_icon_clock for shader", ... },
// textureFormat=10, textureCompression=1, crunchedCompression=0  (1 个名字)
    { "White Square", ... },
// textureFormat=12, textureCompression=1, crunchedCompression=0  (40 个名字)
    { "01-Rook", ... },
    { "02-Veteran", ... },
    { "03-Commander", ... },
    { "04-Admiral", ... },
    { "05-Conqueror", ... },
    { "06-Galactic Threat", ... },
    { "07-Legend", ... },
    { "Alliance_Trophies_AM", ... },
    { "Alliance_Trophies_Black Legion event 1", ... },
    { "Alliance_Trophies_BlackLegion", ... },
    { "Alliance_Trophies_Dark Angels", ... },
    { "Alliance_Trophies_Default_v2", ... },
    { "Alliance_Trophies_Emperors Children", ... },
    { "Alliance_Trophies_GSC", ... },
    { "Alliance_Trophies_Goff", ... },
    { "Alliance_Trophies_Leviathan", ... },
    { "Alliance_Trophies_Ork", ... },
    { "Alliance_Trophies_SaimHann", ... },
    { "Alliance_Trophies_Sautekh", ... },
    { "Alliance_Trophies_Sororitas", ... },
    { "Alliance_Trophies_Space Marine 2 event", ... },
    { "Alliance_Trophies_Space Wolves", ... },
    { "Alliance_Trophies_TIER1", ... },
    { "Alliance_Trophies_TIER2", ... },
    { "Alliance_Trophies_TIER3", ... },
    { "Alliance_Trophies_TIER4", ... },
    { "Alliance_Trophies_Tau", ... },
    { "Alliance_Trophies_Ultramarines", ... },
    { "Alliance_Trophies_Ultramarines event", ... },
    { "Copy@3x", ... },
    { "CrosshairTrail", ... },
    { "LightningTrail", ... },
    { "Roman I", ... },
    { "Roman II", ... },
    { "Roman III", ... },
    { "Roman IV", ... },
    { "Roman V", ... },
    { "Roman VI", ... },
    { "Shine trail", ... },
    { "UI Dirt And Noise skratches", ... },
// textureFormat=10, textureCompression=1, crunchedCompression=1  (6 个名字)
    { "40k_main_GameMode_Skirmish", ... },
    { "Container Image Draft", ... },
    { "Container Image Ranked", ... },
    { "Container Image Tutorial", ... },
    { "Space Wolves Container 1x2", ... },
    { "UM Guilliman Container 1x2", ... },
// textureFormat=12, textureCompression=1, crunchedCompression=1  (64 个名字)
    { "40K_shop_offer_booster_Space Wolves", ... },
    { "Container Image Practice 1x1", ... },
    { "UI_Deck_Master Zacharial", ... },
    { "UI_Deck_Warlord_Abaddon", ... },
    { "UI_Deck_Warlord_Abaddon_AA_HB", ... },
    { "UI_Deck_Warlord_Acolyte Iconward", ... },
    { "UI_Deck_Warlord_Anvirr Keltoc", ... },
    { "UI_Deck_Warlord_Asmodai", ... },
    { "UI_Deck_Warlord_AunVa", ... },
    { "UI_Deck_Warlord_Azrael", ... },
    { "UI_Deck_Warlord_AzraelAA", ... },
    { "UI_Deck_Warlord_Beastboss Morgrim", ... },
    { "UI_Deck_Warlord_Belial", ... },
    { "UI_Deck_Warlord_Boss Zagstruk", ... },
    { "UI_Deck_Warlord_Celestian Sacresant Aveline", ... },
    { "UI_Deck_Warlord_Chaplain Letharius", ... },
    { "UI_Deck_Warlord_Commander OMaisos", ... },
    { "UI_Deck_Warlord_Commissar Deckler", ... },
    { "UI_Deck_Warlord_Daemon Prince", ... },
    { "UI_Deck_Warlord_Eliac Zephyrblade", ... },
    { "UI_Deck_Warlord_Erika Luminas", ... },
    { "UI_Deck_Warlord_Ghazghkull Thraka", ... },
    { "UI_Deck_Warlord_Ghazghkull_AA_HB", ... },
    { "UI_Deck_Warlord_Gordrang", ... },
    { "UI_Deck_Warlord_Grukk Face-Rippa", ... },
    { "UI_Deck_Warlord_Guilliman", ... },
    { "UI_Deck_Warlord_Haarken Worldclaimer", ... },
    { "UI_Deck_Warlord_Hektor Thenmann", ... },
    { "UI_Deck_Warlord_Imotekh the Stormlord", ... },
    { "UI_Deck_Warlord_Imotekh_AA_HB", ... },
    { "UI_Deck_Warlord_Jain Zar", ... },
    { "UI_Deck_Warlord_Junith Eruita", ... },
    { "UI_Deck_Warlord_Leontus", ... },
    { "UI_Deck_Warlord_Lhaska Szenari", ... },
    { "UI_Deck_Warlord_Lhykhis", ... },
    { "UI_Deck_Warlord_Logan Grimnar", ... },
    { "UI_Deck_Warlord_Logan_AA_HB", ... },
    { "UI_Deck_Warlord_Lord Exultant", ... },
    { "UI_Deck_Warlord_Lucius", ... },
    { "UI_Deck_Warlord_Magus", ... },
    { "UI_Deck_Warlord_Marneus Calgar", ... },
    { "UI_Deck_Warlord_Medreyal Ghaelyn", ... },
    { "UI_Deck_Warlord_Morvenn Vahl", ... },
    { "UI_Deck_Warlord_Mozrog Skragbad", ... },
    { "UI_Deck_Warlord_Nemesor Zahndrekh", ... },
    { "UI_Deck_Warlord_Neurothrope", ... },
    { "UI_Deck_Warlord_Njal Stormcaller", ... },
    { "UI_Deck_Warlord_Orikan the Diviner", ... },
    { "UI_Deck_Warlord_Ragnar Blackmane", ... },
    { "UI_Deck_Warlord_Ramatekh", ... },
    { "UI_Deck_Warlord_Saffa Rhiannor", ... },
    { "UI_Deck_Warlord_Shadowsun", ... },
    { "UI_Deck_Warlord_Swarmlord", ... },
    { "UI_Deck_Warlord_Sylar Hexcorn", ... },
    { "UI_Deck_Warlord_Tempestor Prime", ... },
    { "UI_Deck_Warlord_Tervigon", ... },
    { "UI_Deck_Warlord_Titus", ... },
    { "UI_Deck_Warlord_Titus_AA_HB", ... },
    { "UI_Deck_Warlord_Uriel Ventris", ... },
    { "UI_Deck_Warlord_Ursula Creed", ... },
    { "UI_Deck_Warlord_Ursula_AA_HB", ... },
    { "UI_Deck_Warlord_Valius Paxor", ... },
    { "UI_Deck_Warlord_Varro Tigurius", ... },
    { "UI_Deck_Warlord_Winged Tyranid Prime", ... },
```

> 另外：`ArtBaker.ApplyImportSettings` 的 `isSdf` 分支（`/card_sdf/`）与这批不冲突；本轮的 `40k_Rewards_icon_base_card_SDF` / `40k_battle_Win_Skull_SDF` / `OctagonUI_Border_SDF_2` 等**不在 `card_sdf/` 目录**，走的是普通分支。

---

## 7 · 顺手发现（未改，白名单之外）

1. **`B136` / R9 的「138 张可对回」= 精确名一级**；改名级还有 **96 张**（96/96 尺寸相等）也是同一批原版图 —— 本报告已把其中 66 张纳入改动。**建议把 `B136` #7 的口径补成「234 张可对回」**（数字回填不在本件白名单）。
2. **`Resources/Art/` 整个目录在 `.gitignore:124` 里**（`git check-ignore` 现读命中）⇒ **`.meta` 改动不会出现在 `git status` / `git diff`**。验收请直接读文件，⛔ 别用 `git status` 判「改了没有」。（同一条也解释了为什么本轮无法用 `git diff --numstat` 报行数。）
3. **`40K_shop_offer_bg_SW`（450×512 vs 原版 512²）是 15-08 的问题**，本轮按名命中它但 `textureCompression` 已是 1 ⇒ 未改；它若是将来按 15-08 补成 512²，压缩档也应回到原版 DXT5（本轮已确认原版 `fmt 12`）。
4. **本批没有改 `mipmaps` / `maxTextureSize` / `wrap`** —— 那些分别归 15-03 / 15-04 / 15-05 与 W1；本轮只动压缩档 4 键。
5. **`40k_Rewards_icon_base_card_SDF`（原版 fmt 3 = RGB24）可能是本轮唯一「档位对、像素格式仍差」的一张**：我们的 PNG 是 **RGBA（alpha 恒 255）**，`Uncompressed` 导入后 Unity 给的是 **RGBA32** 还是 **RGB24** 取决于 `alphaUsage`（现读 `1`），本轮无 Unity **不能断言**。两者都是未压缩（4 字节 vs 3 字节/像素），不是压缩损失。
6. **`White_Square`（原版 DXT1）的 alpha 已核**：PIL 现读 `getextrema() = (255, 255)` ⇒ **DXT1 不会丢任何 alpha**，安全。
7. **`UI_Deck_Warlord_*` 这批 2048² 立绘的原版是 DXT5Crunched**（`fmt 29`）：本轮把它们从 BC7 降到「DXT5 + Crunch」，与 `ArtBaker.cs:478-493` 的 2048 尺寸策略不冲突（那一段只管 `maxTextureSize`，本轮未动尺寸）。

---

## 8 · 交接给调度台

1. **要不要保留改名级那 66 张**：本轮已改（有备份）。若口径只认 71，回滚 `targets_new.json` 那 66 份即可（`_tmp_view/b141_w40/backup/`）。
2. **必须派 `ArtBaker.cs` 写手**补 `CompTruths`（§6），否则下次烘焙静默打回；这是本件**唯一的外部依赖**。
3. **生效需要一次 Unity 导入**（`AssetDatabase.Refresh` / 打开工程 / 一次烘焙 *之前* 先完成 §6）—— ⛔ **不要先跑 `ArtBaker.BakeFromCLI`**，那会覆盖本轮改动。
4. **无法在本轮验证的**：Unity 是否接受 `platformSettings.textureFormat` 的显式值（12/10）与 `crunchedCompression: 1` 的组合 —— 需一次导入后读回 `.meta` / 导入后的 `Texture2D.format` 确认。⛔ 本轮没有 Unity，不能声称已验证。
