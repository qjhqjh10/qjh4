# B141 · W130 · 15-09（续）· 把 W40 的 UI 压缩真值并进 `ArtBaker.CompTruths`

> 执行写手 `B141-W50` · 会话 128 · 2026-11-04
> 白名单：**只改了** `MyGame/Assets/CardPresentation/Editor/ArtBaker.cs` + 本报告。
> ⛔ 没跑 Unity（`ArtBaker.BakeFromCLI` / 任何 `-executeMethod` 一次都没跑）· ⛔ 没动 git · ⛔ 没碰 `d:/2` · ⛔ 没碰任何 `.meta` / `.png` / 账 / 活状态表 / 所有权表 / CLAUDE.md。

> 📌 **派单里的路径是错的**：写的是 `WarpforgeArena1/Editor/ArtBaker.cs`，本机**不存在**；
> 真身 = `MyGame/Assets/CardPresentation/Editor/ArtBaker.cs`（`find . -name ArtBaker.cs` 现读，全仓只有这一份活的，另一份在 `_tmp_view/b111_t1c/bak/`）。
> 这是 CLAUDE.md「施工单里的文件路径一律现读确认」那条的又一例。

## 0 · 一句话结果

W40 改过的 **137 个 `.meta`（136 个唯一原版名）** 已并进 `CompTruths`（**8 → 144 条**），`ArtCompressionTruth` 补了 `Crunched` 字段，
`ApplyCompressionTruth` 现在把**四个键**（`textureFormat` / `textureCompression` / `crunchedCompression` / `overridden`）落到 **Default + Standalone 两个块**。
静态校验：**137/137 逐张与盘上 `.meta` 的两块四键完全一致 ⇒ 0 不符**；`typecheck` **0 / 0**。

⚠️ **一处判据修正（调度台必看）**：`报告/B141_W130_UICompression.md` §6 写着「`OriginalCompression` 已经会按『原版名 / 下划线形态』查，**无需再改查表逻辑**」——**这条不成立**。
原版资产名**混用两种分隔符**（`UI_Deck_Warlord_Acolyte Iconward`、`40K_shop_offer_booster_Space Wolves`），
而 `OriginalNameCandidates` 只产出「**全下划线**」和「**全空格**」两种形态 ⇒ **136 个名字里 44 个对不上（137 个文件只命中 93）**。
已加**第三档** `CanonName`（去掉 `_` 与空格、忽略大小写）⇒ 三档合计 **136/136 全命中、0 歧义**（144 条表项之间**无规范名碰撞**，实测 0）。

## 1 · 改了 `ArtBaker.cs` 的六个地方

| # | 位置（现读行号） | 改了什么 | 为什么 |
|---|---|---|---|
| ① | `:940-946`（`struct ArtCompressionTruth`） | 新增 `public bool Crunched;`（`:945`） | 70 张（DXT5C 64 + DXT1C 6）要写 `crunchedCompression: 1`；原结构体表达不了 |
| ② | `:961-1112`（`CompTruths` 内） | 追加 **136 条** UI 切片真值（分 5 组，见 §2） | 不并入 ⇒ 下次烘焙被 `ApplyImportSettings` 无条件写回 `CompressedHQ`，**静默打回** |
| ③ | `:1115-1134` | 三个档位构造器 `UiUncompressed()` / `UiDxt1(bool)` / `UiDxt5(bool)` + **规范名索引** `CompTruthsCanonical()` + `CanonName()` | 136 条若逐条写长构造器，一行 150 字没法核；规范名索引解决混合分隔符 |
| ④ | `:1136-1149`（`OriginalCompression`） | 查表**三档**：① 精确名 ② `OriginalNameCandidates`（全下划线/全空格）③ `CanonName` | 见 §0 的判据修正 |
| ⑤ | `:1151-1183`（`ApplyCompressionTruth`） | **Default 块也写**（先顶层 compression/crunched，再 `Get/SetDefaultPlatformTextureSettings` 整份落笔，保证 `format` 不被别的 setter 重置）+ Standalone 块补 `crunchedCompression` / `format` | 原来只写 Standalone 块 ⇒ `.meta` 的 `DefaultTexturePlatform` 会停在 `-1 / 2`(BC7)，与原版不符 |
| ⑥ | `:648`（`ApplyArenaImportSettings` 那一支） | 补一行 `ps.crunchedCompression = ctruth.Crunched;` | 表与 arena 共用 ⇒ 结构体加了字段就要在两个消费点都落地（现表内 8 条都是 `false` ⇒ **零行为变化**） |

- 文件 **991 → 1193 行（+202）**；**纯 CRLF 保持**（改后 `CRLF 1193 / LF 1193 / 裸 CR 0`，二进制现读）。
- **账的落点不受影响**：`grep -rn "ArtBaker" Unity/账/` = **0 命中**（本文件从来没被账按行号引用过）。
- ✅ 标记已打在 `:961`（新增段首）与 `:648`（arena 那一支），格式 `// ✅ 已复刻（2026-11-04 · B141-W50 · 15-09 · 判据 = …）`。

### 1·1 🔴 第一版写法是错的（自己抓到、已改）：`ti.textureFormat` 已 **obsolete(CS0618)**，而且它不直写那一格

第一版把 Default 块写成 `ti.textureFormat = (TextureImporterFormat)…`。**`typecheck` 报 0 错**（它只 `grep "error CS"`，**不打印 warning**），
是我另外**直接跑 `csc @wf_csc_editor.rsp` 并 grep `ArtBaker.cs.*warning`** 才看见的：

```
Assets\CardPresentation\Editor\ArtBaker.cs(1162,9): warning CS0618: 「TextureImporter.textureFormat」已过时:
「textureFormat is no longer accessible at the TextureImporter level. … For platform specific formats use the
[[PlatformTextureSettings]] API. Using this setter will setup various parameters to match the new automatic system
as well as possible. Getter will return the last value set.」
```

⇒ 那个 setter **会顺带改别的键**（原文「setup various parameters to match the new automatic system」），不是「直写 `textureFormat` 那一格」。
已改成 **`GetDefaultPlatformTextureSettings()` → 改四键 → `SetPlatformTextureSettings(def)`**（Unity 自己在 obsolete 文案里指的就是这条 API），
并从 `ti.maxTextureSize` 回填 `def.maxTextureSize`（调用方刚设过 2048，别被旧快照盖回 1024）。
改完再跑同一份 `csc`：**ArtBaker.cs 一条 warning 都没有**。

## 2 · 并入的是什么（136 名 / 5 档 / 137 个文件）

名字**不是手抄的**：从 `_tmp_view/b141_w40/targets_all.json`（W40 自己那份 137 条逐张清单）按 `fmt` 分组机生成，再核对名字集合 ——
`W40 名字集 - 表内 = 0`（`表内 - W40 名字集 = 旧的 8 条 arena 条目`，符合预期）。

| 原版 `m_TextureFormat` | 目标档 | 条目数 | 盘上 `.meta` 的 Default 块 | Standalone 块 |
|---|---|---|---|---|
| 3 RGB24 / 4 RGBA32 | `Uncompressed` · `ExplicitFormat = -1` | **25** | `-1 / 0 / 0 / ovr 0` | `-1 / 0 / 0 / ovr 0` |
| 10 DXT1 | `Compressed` · 显式 `DXT1` | **1** | `10 / 1 / 0 / ovr 0` | `10 / 1 / 0 / **ovr 1**` |
| 28 DXT1Crunched | `Compressed` · 显式 `DXT1` · `Crunched` | **6** | `10 / 1 / 1 / ovr 0` | `10 / 1 / 1 / **ovr 1**` |
| 12 DXT5 | `Compressed` · 显式 `DXT5` | **40** | `12 / 1 / 0 / ovr 0` | `12 / 1 / 0 / **ovr 1**` |
| 29 DXT5Crunched | `Compressed` · 显式 `DXT5` · `Crunched` | **64** | `12 / 1 / 1 / ovr 0` | `12 / 1 / 1 / **ovr 1**` |
| | | **136 名 / 137 文件** | | |

> `40k_popup_texture` 同时存在于 `ui/` 与 `ui_menu/`（137 = 136 + 这 1 个重名）—— 两条都命中同一条真值，实测两个文件也都一致。

**Crunch 字段形状**（`:940-946`）：

```csharp
struct ArtCompressionTruth
{
    public TextureImporterCompression Compression;
    public int ExplicitFormat;   // -1 = 不指定显式平台格式（走压缩档）；否则 = (int)TextureImporterFormat
    /// <summary>原版 `m_TextureFormat` 是 *Crunched（28 DXT1C / 29 DXT5C）时 = true ⇒ 写 `crunchedCompression: 1`（B141 · 15-09）。</summary>
    public bool Crunched;
}
```

档位构造器（`:1116-1118`，让 136 行每行都能一眼核）：

```csharp
static ArtCompressionTruth UiUncompressed()      => new ArtCompressionTruth { Compression = TextureImporterCompression.Uncompressed, ExplicitFormat = -1, Crunched = false };
static ArtCompressionTruth UiDxt1(bool crunched) => new ArtCompressionTruth { Compression = TextureImporterCompression.Compressed, ExplicitFormat = (int)TextureImporterFormat.DXT1, Crunched = crunched };
static ArtCompressionTruth UiDxt5(bool crunched) => new ArtCompressionTruth { Compression = TextureImporterCompression.Compressed, ExplicitFormat = (int)TextureImporterFormat.DXT5, Crunched = crunched };
```

## 3 · 验证

### 3·1 typecheck

```
$ TMPDIR=/tmp/wf_b141_w50 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- 我改的文件 **0 错**。
- 确认 `ArtBaker.cs` 出现在 `wf_csc_editor.rsp` 的源清单里（不是拿旧产物糊过去）。
- ⚠️ **`typecheck.sh` 默认档只打印 `error CS`，不打印 warning** ⇒ 「0 错」**不能证明「没有 obsolete 告警」**。
  所以另外直跑了一次 `csc @wf_csc_editor.rsp` 并 `grep "ArtBaker.cs.*warning"`：
  **第一版**报了 1 条 `CS0618 TextureImporter.textureFormat 已过时`（详见 §1·1，已改掉），**改完这版是 0 条 warning**。

### 3·2 静态逐张校验：**137/137 一致，0 不符**

口径（只读、可复跑：`python -I -X utf8 d:/4/_tmp_view/b141_w50/verify.py`；逻辑镜像 `ArtBaker`，**真值表直接从 `.cs` 源码解析**，不复制第二份名单）：

1. 正则解析 `CompTruths`（两种写法都认）⇒ **144 条**；`CanonName` 碰撞检查 ⇒ **0**。
2. 镜像 `OriginalNameCandidates` + `CanonName` 三档查表。
3. 逐个读盘上 `.meta` 的 `platformSettings` 两个块，比 **`textureFormat` / `textureCompression` / `crunchedCompression` / `overridden`**。

> 产物留档：`_tmp_view/b141_w50/gen_entries.py` → `entries.txt`（= 并进 `CompTruths` 的那 136 行）· `verify.py` → `rows.md`（= §3·3 那张表）。

结果：

| 项 | 值 |
|---|---|
| 比对文件数 | **137** |
| 不符（任一键） | **0** |
| 命中方式 | `exact`（精确名/全下划线/全空格）**71** · `canon`（混合分隔符）**66** |
| 档位分布（现读） | `(-1,0,0)`×26 · `(10,1,0)`×1 · `(10,1,1)`×6 · `(12,1,0)`×40 · `(12,1,1)`×64 |
| `Default.overridden` | 137/137 = **0**（代码不写这一键，实测现值就是 0） |
| `Standalone.overridden` | 显式格式的 111 张 = **1**；未压缩 26 张 = **0** |

> `71 = exact` 与本轮判据里那个「精确匹配级 71 张」**逐数吻合**，是口径自洽的旁证。

### 3·3 逐张清单（137 行，现读值 = 期望值）

| # | 文件（`Resources/Art/…` / `arenas/…`） | 原版名 | 命中 | Default fmt/comp/crunch | Standalone fmt/comp/crunch/ovr | 一致 |
|---|---|---|---|---|---|---|
| 1 | `ui_menu/40k_Rewards_icon_base_card_SDF.png.meta` | `40k_Rewards_icon_base card SDF` | canon | -1/0/0 | -1/0/0/0 | ✅ |
| 2 | `ui/40k_popup_texture.png.meta` | `40k_popup_texture` | exact | -1/0/0 | -1/0/0/0 | ✅ |
| 3 | `ui/40k_voicelines_radio_wave_equalizer.png.meta` | `40k_voicelines_radio_wave equalizer` | canon | -1/0/0 | -1/0/0/0 | ✅ |
| 4 | `ui_deck/2_40k_cardframe_rarity_rare.png.meta` | `2_40k_cardframe_rarity_rare` | exact | -1/0/0 | -1/0/0/0 | ✅ |
| 5 | `ui_deck/40k_DeckSelection_icon_FactionBlackLegion.png.meta` | `40k_DeckSelection_icon_FactionBlackLegion` | exact | -1/0/0 | -1/0/0/0 | ✅ |
| 6 | `ui_deck/40k_DeckSelection_icon_FactionDarkAngels.png.meta` | `40k_DeckSelection_icon_FactionDarkAngels` | exact | -1/0/0 | -1/0/0/0 | ✅ |
| 7 | `ui_deck/40k_DeckSelection_icon_FactionUM.png.meta` | `40k_DeckSelection_icon_FactionUM` | exact | -1/0/0 | -1/0/0/0 | ✅ |
| 8 | `ui_deck/Card_Text_smooth_background.png.meta` | `Card Text smooth background` | canon | -1/0/0 | -1/0/0/0 | ✅ |
| 9 | `ui_deck/Smooth_background_square.png.meta` | `Smooth background square` | canon | -1/0/0 | -1/0/0/0 | ✅ |
| 10 | `ui_menu/40K_ArmyTrack_bar_position.png.meta` | `40K_ArmyTrack_bar_position` | exact | -1/0/0 | -1/0/0/0 | ✅ |
| 11 | `ui_menu/40k_Generic_Smooth_line.png.meta` | `40k_Generic Smooth line` | canon | -1/0/0 | -1/0/0/0 | ✅ |
| 12 | `ui_menu/40k_battle_Win_Skull_SDF.png.meta` | `40k_battle_Win Skull SDF` | canon | -1/0/0 | -1/0/0/0 | ✅ |
| 13 | `ui_menu/40k_popup_texture.png.meta` | `40k_popup_texture` | exact | -1/0/0 | -1/0/0/0 | ✅ |
| 14 | `ui_menu/40k_shop_popup_info_bg.png.meta` | `40k_shop_popup_info_bg` | exact | -1/0/0 | -1/0/0/0 | ✅ |
| 15 | `ui_menu/40k_topmarquee_currency_blackstone.png.meta` | `40k_topmarquee_currency_blackstone` | exact | -1/0/0 | -1/0/0/0 | ✅ |
| 16 | `ui_menu/40k_topmarquee_currency_crystal.png.meta` | `40k_topmarquee_currency_crystal` | exact | -1/0/0 | -1/0/0/0 | ✅ |
| 17 | `ui_menu/40k_topmarquee_currency_energy.png.meta` | `40k_topmarquee_currency_energy` | exact | -1/0/0 | -1/0/0/0 | ✅ |
| 18 | `ui_menu/40k_topmarquee_currency_gold.png.meta` | `40k_topmarquee_currency_gold` | exact | -1/0/0 | -1/0/0/0 | ✅ |
| 19 | `ui_menu/40k_topmarquee_currency_ticket.png.meta` | `40k_topmarquee_currency_ticket` | exact | -1/0/0 | -1/0/0/0 | ✅ |
| 20 | `ui_menu/Background.png.meta` | `Background` | exact | -1/0/0 | -1/0/0/0 | ✅ |
| 21 | `ui_menu/Crate_Border_Highlight.png.meta` | `Crate Border Highlight` | canon | -1/0/0 | -1/0/0/0 | ✅ |
| 22 | `ui_menu/Glow_UI_W40K.png.meta` | `Glow UI W40K` | canon | -1/0/0 | -1/0/0/0 | ✅ |
| 23 | `ui_menu/InputFieldBackground.png.meta` | `InputFieldBackground` | exact | -1/0/0 | -1/0/0/0 | ✅ |
| 24 | `ui_menu/OctagonUI_Border_SDF_2.png.meta` | `OctagonUI Border SDF 2` | canon | -1/0/0 | -1/0/0/0 | ✅ |
| 25 | `ui_menu/Tutorial_Highlight.png.meta` | `Tutorial Highlight` | canon | -1/0/0 | -1/0/0/0 | ✅ |
| 26 | `ui_menu/WF_icon_clock_for_shader.png.meta` | `WF_icon_clock for shader` | canon | -1/0/0 | -1/0/0/0 | ✅ |
| 27 | `ui/White_Square.png.meta` | `White Square` | canon | 10/1/0 | 10/1/0/1 | ✅ |
| 28 | `ui/CrosshairTrail.png.meta` | `CrosshairTrail` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 29 | `ui_menu/01-Rook.png.meta` | `01-Rook` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 30 | `ui_menu/02-Veteran.png.meta` | `02-Veteran` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 31 | `ui_menu/03-Commander.png.meta` | `03-Commander` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 32 | `ui_menu/04-Admiral.png.meta` | `04-Admiral` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 33 | `ui_menu/05-Conqueror.png.meta` | `05-Conqueror` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 34 | `ui_menu/06-Galactic_Threat.png.meta` | `06-Galactic Threat` | canon | 12/1/0 | 12/1/0/1 | ✅ |
| 35 | `ui_menu/07-Legend.png.meta` | `07-Legend` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 36 | `ui_menu/Alliance_Trophies_AM.png.meta` | `Alliance_Trophies_AM` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 37 | `ui_menu/Alliance_Trophies_Black Legion event 1.png.meta` | `Alliance_Trophies_Black Legion event 1` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 38 | `ui_menu/Alliance_Trophies_BlackLegion.png.meta` | `Alliance_Trophies_BlackLegion` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 39 | `ui_menu/Alliance_Trophies_Dark Angels.png.meta` | `Alliance_Trophies_Dark Angels` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 40 | `ui_menu/Alliance_Trophies_Default_v2.png.meta` | `Alliance_Trophies_Default_v2` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 41 | `ui_menu/Alliance_Trophies_Emperors Children.png.meta` | `Alliance_Trophies_Emperors Children` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 42 | `ui_menu/Alliance_Trophies_GSC.png.meta` | `Alliance_Trophies_GSC` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 43 | `ui_menu/Alliance_Trophies_Goff.png.meta` | `Alliance_Trophies_Goff` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 44 | `ui_menu/Alliance_Trophies_Leviathan.png.meta` | `Alliance_Trophies_Leviathan` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 45 | `ui_menu/Alliance_Trophies_Ork.png.meta` | `Alliance_Trophies_Ork` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 46 | `ui_menu/Alliance_Trophies_SaimHann.png.meta` | `Alliance_Trophies_SaimHann` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 47 | `ui_menu/Alliance_Trophies_Sautekh.png.meta` | `Alliance_Trophies_Sautekh` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 48 | `ui_menu/Alliance_Trophies_Sororitas.png.meta` | `Alliance_Trophies_Sororitas` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 49 | `ui_menu/Alliance_Trophies_Space Marine 2 event.png.meta` | `Alliance_Trophies_Space Marine 2 event` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 50 | `ui_menu/Alliance_Trophies_Space Wolves.png.meta` | `Alliance_Trophies_Space Wolves` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 51 | `ui_menu/Alliance_Trophies_TIER1.png.meta` | `Alliance_Trophies_TIER1` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 52 | `ui_menu/Alliance_Trophies_TIER2.png.meta` | `Alliance_Trophies_TIER2` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 53 | `ui_menu/Alliance_Trophies_TIER3.png.meta` | `Alliance_Trophies_TIER3` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 54 | `ui_menu/Alliance_Trophies_TIER4.png.meta` | `Alliance_Trophies_TIER4` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 55 | `ui_menu/Alliance_Trophies_Tau.png.meta` | `Alliance_Trophies_Tau` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 56 | `ui_menu/Alliance_Trophies_Ultramarines event.png.meta` | `Alliance_Trophies_Ultramarines event` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 57 | `ui_menu/Alliance_Trophies_Ultramarines.png.meta` | `Alliance_Trophies_Ultramarines` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 58 | `ui_menu/Copy@3x.png.meta` | `Copy@3x` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 59 | `ui_menu/LightningTrail.png.meta` | `LightningTrail` | exact | 12/1/0 | 12/1/0/1 | ✅ |
| 60 | `ui_menu/Roman_I.png.meta` | `Roman I` | canon | 12/1/0 | 12/1/0/1 | ✅ |
| 61 | `ui_menu/Roman_II.png.meta` | `Roman II` | canon | 12/1/0 | 12/1/0/1 | ✅ |
| 62 | `ui_menu/Roman_III.png.meta` | `Roman III` | canon | 12/1/0 | 12/1/0/1 | ✅ |
| 63 | `ui_menu/Roman_IV.png.meta` | `Roman IV` | canon | 12/1/0 | 12/1/0/1 | ✅ |
| 64 | `ui_menu/Roman_V.png.meta` | `Roman V` | canon | 12/1/0 | 12/1/0/1 | ✅ |
| 65 | `ui_menu/Roman_VI.png.meta` | `Roman VI` | canon | 12/1/0 | 12/1/0/1 | ✅ |
| 66 | `ui_menu/Shine_trail.png.meta` | `Shine trail` | canon | 12/1/0 | 12/1/0/1 | ✅ |
| 67 | `ui_menu/UI_Dirt_And_Noise_skratches.png.meta` | `UI Dirt And Noise skratches` | canon | 12/1/0 | 12/1/0/1 | ✅ |
| 68 | `ui_menu/40k_main_GameMode_Skirmish.png.meta` | `40k_main_GameMode_Skirmish` | exact | 10/1/1 | 10/1/1/1 | ✅ |
| 69 | `ui_menu/Container_Image_Draft.png.meta` | `Container Image Draft` | canon | 10/1/1 | 10/1/1/1 | ✅ |
| 70 | `ui_menu/Container_Image_Ranked.png.meta` | `Container Image Ranked` | canon | 10/1/1 | 10/1/1/1 | ✅ |
| 71 | `ui_menu/Container_Image_Tutorial.png.meta` | `Container Image Tutorial` | canon | 10/1/1 | 10/1/1/1 | ✅ |
| 72 | `ui_menu/Space_Wolves_Container_1x2.png.meta` | `Space Wolves Container 1x2` | canon | 10/1/1 | 10/1/1/1 | ✅ |
| 73 | `ui_menu/UM_Guilliman_Container_1x2.png.meta` | `UM Guilliman Container 1x2` | canon | 10/1/1 | 10/1/1/1 | ✅ |
| 74 | `ui_menu/40K_shop_offer_booster_Space_Wolves.png.meta` | `40K_shop_offer_booster_Space Wolves` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 75 | `ui_menu/Container_Image_Practice_1x1.png.meta` | `Container Image Practice 1x1` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 76 | `ui_menu/UI_Deck_Master_Zacharial.png.meta` | `UI_Deck_Master Zacharial` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 77 | `ui_menu/UI_Deck_Warlord_Abaddon.png.meta` | `UI_Deck_Warlord_Abaddon` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 78 | `ui_menu/UI_Deck_Warlord_Abaddon_AA_HB.png.meta` | `UI_Deck_Warlord_Abaddon_AA_HB` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 79 | `ui_menu/UI_Deck_Warlord_Acolyte_Iconward.png.meta` | `UI_Deck_Warlord_Acolyte Iconward` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 80 | `ui_menu/UI_Deck_Warlord_Anvirr_Keltoc.png.meta` | `UI_Deck_Warlord_Anvirr Keltoc` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 81 | `ui_menu/UI_Deck_Warlord_Asmodai.png.meta` | `UI_Deck_Warlord_Asmodai` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 82 | `ui_menu/UI_Deck_Warlord_AunVa.png.meta` | `UI_Deck_Warlord_AunVa` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 83 | `ui_menu/UI_Deck_Warlord_Azrael.png.meta` | `UI_Deck_Warlord_Azrael` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 84 | `ui_menu/UI_Deck_Warlord_AzraelAA.png.meta` | `UI_Deck_Warlord_AzraelAA` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 85 | `ui_menu/UI_Deck_Warlord_Beastboss_Morgrim.png.meta` | `UI_Deck_Warlord_Beastboss Morgrim` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 86 | `ui_menu/UI_Deck_Warlord_Belial.png.meta` | `UI_Deck_Warlord_Belial` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 87 | `ui_menu/UI_Deck_Warlord_Boss_Zagstruk.png.meta` | `UI_Deck_Warlord_Boss Zagstruk` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 88 | `ui_menu/UI_Deck_Warlord_Celestian_Sacresant_Aveline.png.meta` | `UI_Deck_Warlord_Celestian Sacresant Aveline` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 89 | `ui_menu/UI_Deck_Warlord_Chaplain_Letharius.png.meta` | `UI_Deck_Warlord_Chaplain Letharius` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 90 | `ui_menu/UI_Deck_Warlord_Commander_OMaisos.png.meta` | `UI_Deck_Warlord_Commander OMaisos` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 91 | `ui_menu/UI_Deck_Warlord_Commissar_Deckler.png.meta` | `UI_Deck_Warlord_Commissar Deckler` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 92 | `ui_menu/UI_Deck_Warlord_Daemon_Prince.png.meta` | `UI_Deck_Warlord_Daemon Prince` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 93 | `ui_menu/UI_Deck_Warlord_Eliac_Zephyrblade.png.meta` | `UI_Deck_Warlord_Eliac Zephyrblade` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 94 | `ui_menu/UI_Deck_Warlord_Erika_Luminas.png.meta` | `UI_Deck_Warlord_Erika Luminas` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 95 | `ui_menu/UI_Deck_Warlord_Ghazghkull_AA_HB.png.meta` | `UI_Deck_Warlord_Ghazghkull_AA_HB` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 96 | `ui_menu/UI_Deck_Warlord_Ghazghkull_Thraka.png.meta` | `UI_Deck_Warlord_Ghazghkull Thraka` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 97 | `ui_menu/UI_Deck_Warlord_Gordrang.png.meta` | `UI_Deck_Warlord_Gordrang` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 98 | `ui_menu/UI_Deck_Warlord_Grukk_Face-Rippa.png.meta` | `UI_Deck_Warlord_Grukk Face-Rippa` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 99 | `ui_menu/UI_Deck_Warlord_Guilliman.png.meta` | `UI_Deck_Warlord_Guilliman` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 100 | `ui_menu/UI_Deck_Warlord_Haarken_Worldclaimer.png.meta` | `UI_Deck_Warlord_Haarken Worldclaimer` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 101 | `ui_menu/UI_Deck_Warlord_Hektor_Thenmann.png.meta` | `UI_Deck_Warlord_Hektor Thenmann` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 102 | `ui_menu/UI_Deck_Warlord_Imotekh_AA_HB.png.meta` | `UI_Deck_Warlord_Imotekh_AA_HB` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 103 | `ui_menu/UI_Deck_Warlord_Imotekh_the_Stormlord.png.meta` | `UI_Deck_Warlord_Imotekh the Stormlord` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 104 | `ui_menu/UI_Deck_Warlord_Jain_Zar.png.meta` | `UI_Deck_Warlord_Jain Zar` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 105 | `ui_menu/UI_Deck_Warlord_Junith_Eruita.png.meta` | `UI_Deck_Warlord_Junith Eruita` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 106 | `ui_menu/UI_Deck_Warlord_Leontus.png.meta` | `UI_Deck_Warlord_Leontus` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 107 | `ui_menu/UI_Deck_Warlord_Lhaska_Szenari.png.meta` | `UI_Deck_Warlord_Lhaska Szenari` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 108 | `ui_menu/UI_Deck_Warlord_Lhykhis.png.meta` | `UI_Deck_Warlord_Lhykhis` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 109 | `ui_menu/UI_Deck_Warlord_Logan_AA_HB.png.meta` | `UI_Deck_Warlord_Logan_AA_HB` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 110 | `ui_menu/UI_Deck_Warlord_Logan_Grimnar.png.meta` | `UI_Deck_Warlord_Logan Grimnar` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 111 | `ui_menu/UI_Deck_Warlord_Lord_Exultant.png.meta` | `UI_Deck_Warlord_Lord Exultant` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 112 | `ui_menu/UI_Deck_Warlord_Lucius.png.meta` | `UI_Deck_Warlord_Lucius` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 113 | `ui_menu/UI_Deck_Warlord_Magus.png.meta` | `UI_Deck_Warlord_Magus` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 114 | `ui_menu/UI_Deck_Warlord_Marneus_Calgar.png.meta` | `UI_Deck_Warlord_Marneus Calgar` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 115 | `ui_menu/UI_Deck_Warlord_Medreyal_Ghaelyn.png.meta` | `UI_Deck_Warlord_Medreyal Ghaelyn` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 116 | `ui_menu/UI_Deck_Warlord_Morvenn_Vahl.png.meta` | `UI_Deck_Warlord_Morvenn Vahl` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 117 | `ui_menu/UI_Deck_Warlord_Mozrog_Skragbad.png.meta` | `UI_Deck_Warlord_Mozrog Skragbad` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 118 | `ui_menu/UI_Deck_Warlord_Nemesor_Zahndrekh.png.meta` | `UI_Deck_Warlord_Nemesor Zahndrekh` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 119 | `ui_menu/UI_Deck_Warlord_Neurothrope.png.meta` | `UI_Deck_Warlord_Neurothrope` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 120 | `ui_menu/UI_Deck_Warlord_Njal_Stormcaller.png.meta` | `UI_Deck_Warlord_Njal Stormcaller` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 121 | `ui_menu/UI_Deck_Warlord_Orikan_the_Diviner.png.meta` | `UI_Deck_Warlord_Orikan the Diviner` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 122 | `ui_menu/UI_Deck_Warlord_Ragnar_Blackmane.png.meta` | `UI_Deck_Warlord_Ragnar Blackmane` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 123 | `ui_menu/UI_Deck_Warlord_Ramatekh.png.meta` | `UI_Deck_Warlord_Ramatekh` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 124 | `ui_menu/UI_Deck_Warlord_Saffa_Rhiannor.png.meta` | `UI_Deck_Warlord_Saffa Rhiannor` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 125 | `ui_menu/UI_Deck_Warlord_Shadowsun.png.meta` | `UI_Deck_Warlord_Shadowsun` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 126 | `ui_menu/UI_Deck_Warlord_Swarmlord.png.meta` | `UI_Deck_Warlord_Swarmlord` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 127 | `ui_menu/UI_Deck_Warlord_Sylar_Hexcorn.png.meta` | `UI_Deck_Warlord_Sylar Hexcorn` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 128 | `ui_menu/UI_Deck_Warlord_Tempestor_Prime.png.meta` | `UI_Deck_Warlord_Tempestor Prime` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 129 | `ui_menu/UI_Deck_Warlord_Tervigon.png.meta` | `UI_Deck_Warlord_Tervigon` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 130 | `ui_menu/UI_Deck_Warlord_Titus.png.meta` | `UI_Deck_Warlord_Titus` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 131 | `ui_menu/UI_Deck_Warlord_Titus_AA_HB.png.meta` | `UI_Deck_Warlord_Titus_AA_HB` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 132 | `ui_menu/UI_Deck_Warlord_Uriel_Ventris.png.meta` | `UI_Deck_Warlord_Uriel Ventris` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 133 | `ui_menu/UI_Deck_Warlord_Ursula_AA_HB.png.meta` | `UI_Deck_Warlord_Ursula_AA_HB` | exact | 12/1/1 | 12/1/1/1 | ✅ |
| 134 | `ui_menu/UI_Deck_Warlord_Ursula_Creed.png.meta` | `UI_Deck_Warlord_Ursula Creed` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 135 | `ui_menu/UI_Deck_Warlord_Valius_Paxor.png.meta` | `UI_Deck_Warlord_Valius Paxor` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 136 | `ui_menu/UI_Deck_Warlord_Varro_Tigurius.png.meta` | `UI_Deck_Warlord_Varro Tigurius` | canon | 12/1/1 | 12/1/1/1 | ✅ |
| 137 | `ui_menu/UI_Deck_Warlord_Winged_Tyranid_Prime.png.meta` | `UI_Deck_Warlord_Winged Tyranid Prime` | canon | 12/1/1 | 12/1/1/1 | ✅ |

## 4 · 副作用：**4 个不在 137 名单里、但会因「同名原版图」改档**的文件

`CompTruths` 是**按原版名**键的（`ApplyImportSettings` 与 `ApplyArenaImportSettings` 共用同一张表），
所以同一个原版图在别的目录里的拷贝也会一起命中。逐张核过**尺寸与原版相等 ⇒ 是同一张原版图**（不是同名异物）：

| 文件 | 命中原版名 | 现 Default (fmt/comp/crunch) | 下次烘焙后 | 尺寸 | 路径 |
|---|---|---|---|---|---|
| `boosterpack/40K_shop_offer_booster_Space_Wolves.png` | `40K_shop_offer_booster_Space Wolves` | `-1 / 2 / 0` (BC7) | `12 / 1 / 1` (DXT5C) | 1024² = 原版 1024² | `ApplyImportSettings` |
| `vfx/Shine trail.png` | `Shine trail` | `-1 / 2 / 0` | `12 / 1 / 0` (DXT5) | 128² = 原版 128² | `ApplyImportSettings` |
| `battlearena3/Textures/LightningTrail.png` | `LightningTrail` | `-1 / 2 / 0` | `12 / 1 / 0` | 128² = 原版 128² | `ApplyArenaImportSettings` |
| `battlearenasororitas/Sprites/Card Text smooth background.png` | `Card Text smooth background` | `-1 / 1 / 0` | `-1 / 0 / 0` (未压缩) | 32² = 原版 32² | `ApplyArenaImportSettings` |

- 判断：这 4 张**本来就是同一张原版贴图**（名字 + 尺寸双证）⇒ 按原版档位是**更接近原版**，不是误伤。
- ⚠️ 但它是**本轮白名单之外的行为变化**（`.meta` 不在我这份白名单里，我也没去改它们）⇒ **要不要保留，请调度台定**。
  若要「只对 `Resources/Art/{ui,ui_menu,ui_deck}` 生效」，改法很小：在 `OriginalCompression` 里先判 `path` 前缀、或给 `CompTruths` 拆成两张表 —— **本包没做**（那是设计决策，不该由写手自己拍）。
- 另外 39 个命中表但不在名单里的文件（`MatCap Polish` / `Noise Combined` / `Default-Particle` … 分布在 13 个战场）属于**旧表那 8 条**，**本批之前就会命中**，与我这次改动无关。

## 5 · 顺手发现（**未改**，白名单之外）

1. 🔴 **`mip / wrap / filter / aniso` 那条链有同一个「混合分隔符」缺口，且它有真实判据、现在是白拿不到的**。
   `TryFindTruth` 用的也是 `OriginalNameCandidates`（只有全下划线 / 全空格两档）。实测：
   **这 137 张里同样有 44 张在 `Unity/数据/游戏数据/vfx_texture_mips.tsv` 里查得到真值，却因为名字混合形态而查不到**
   （137 张在 TSV 的命中：**旧候选 93 / 加规范名 137**；两边都查不到的是 **0**）。
   ⇒ 那 44 张的 `mipmapEnabled / wrapMode(U/V) / filterMode / anisoLevel` **目前一律停在现值**，不是原版值。
   修法（若调度台要）：把 `CanonName` 这一档也接进 `TryFindTruth`（同一个文件、约 3 行）。**本包没做** —— 它改的是另一类参数（mip/wrap/filter），
   且会影响 44 个文件的导入结果，属于「判据已定但影响面超出本任务」⇒ 按铁律 14 记账外，还需调度台点头。
2. **`报告/B141_W130_UICompression.md` §6 的两句需要就地订正**（本包无权改那份报告）：
   - 「`OriginalCompression` 已经会按『原版名 / 下划线形态』查，无需再改查表逻辑」→ **错**，见 §0（44/136 漏）。
   - 「本轮的 136 个唯一原版名**不在表内**」→ 并入后已在表内，可改为「已并入（`B141-W50`）」。
3. **W40 说的「`Resources/Art` 在 `.gitignore` 里、`.meta` 改动不进 `git status`」仍然成立**：
   本批同样**不能用 `git status` 验收这 137 张**，只能读文件（§3·3 就是逐份二进制读出来的）。
4. `ApplyImportSettings` 里 `isSdf`（`/card_sdf/`）会**跳过** `ApplyCompressionTruth` —— 本轮 137 张**没有一张**在 `card_sdf/` 下（现读 0 命中），
   `40k_Rewards_icon_base_card_SDF` / `40k_battle_Win_Skull_SDF` / `OctagonUI_Border_SDF_2` 都在 `ui_menu/`，走普通分支，确认无误。

## 6 · ❓ 没有 Unity 就不能断言的三件事（⛔ 不要当成已验证）

1. **`GetDefaultPlatformTextureSettings()` + `SetPlatformTextureSettings(def)` 是否真把 `format` 写进 `.meta` 的 `DefaultTexturePlatform.textureFormat`** ——
   这是 Unity 在 `textureFormat` 的 obsolete 文案里**自己指的**那条 API，但没有一次导入回读。
   下次 `BakeFromCLI` 之后请读回两份 `.meta`：`ui_menu/Background.png.meta` 应仍是 `-1 / 0 / 0`；
   `ui_menu/UI_Deck_Warlord_Titus.png.meta` 应仍是 `12 / 1 / 1`。**若 Default 块的 `textureFormat` 回读成 `-1`**，说明这条 API 对默认平台也不写格式 ⇒ 要换法（那时再议）。
2. **`SetPlatformTextureSettings` 在 `overridden = 0` 时会不会忽略我们对 `format` / `crunched` 的写入** ——
   本轮 26 张未压缩就是这种状态；即便被忽略，它们的目标值与现值相同（§3·3），**不会打回**，但「代码是否真的写了」无法用静态方法证明。
3. **Unity 是否接受 `textureFormat: 12/10 + crunchedCompression: 1` 这个组合**（W40 §8·4 也提过）—— 需要一次导入后读 `Texture2D.format` 才能定。

## 7 · 交接给调度台

1. **可以进下一次烘焙了**（本包没跑）：`ArtBaker.ApplyImportSettings` / `BakeFromCLI` 不会再把这 137 张打回 `CompressedHQ`。
2. **要拍板的一件事**：§4 那 4 个副作用的「同名原版图跨目录命中」保留还是收窄（保留 = 更接近原版；收窄 = 严格只动 UI 三个子树）。
3. **要不要顺手修 §5·1 的 44 张 mip/wrap/filter**（同一个根因、同一个文件、有现成判据）。
4. 三次 Unity 回读项见 §6；回读通过后本任务可核销。
