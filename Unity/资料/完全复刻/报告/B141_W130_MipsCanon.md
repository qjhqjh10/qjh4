# B141 · W130 · `TryFindTruth` 接第三档 `CanonName`（mip / wrap / filter / aniso 的「混合分隔符」缺口）

> 执行写手 `B141-W61` · 会话 128 · 2026-11-04
> 判据来源：`报告/B141_W130_ArtBakerTruth.md` §5·1（W50 顺手发现）+ `报告/B141_W130_UICompression.md`（W40，137 张清单）
> 白名单：**改了两样** —— `MyGame/Assets/CardPresentation/Editor/ArtBaker.cs`（只动 `TryFindTruth` 那一支）+ **1 个** `.meta`（只动四类键）。
> ⛔ 没跑 Unity（`ArtBaker.BakeFromCLI` / 任何 `-executeMethod` 一次都没跑）· ⛔ 没动 git · ⛔ 没碰 `d:/2` · ⛔ 没碰压缩档（`CompTruths` / `ApplyCompressionTruth` 一行未动）· ⛔ 没碰账 / 活状态表 / 所有权表 / CLAUDE.md。

## 0 · 一句话结果

`TryFindTruth` 补上第三档 `CanonName`（去 `_` 与空格、忽略大小写）⇒ **44 张里 44 张现在全部查得到真值**（改前 0/44）。
逐张对盘上 `.meta`：**43 张早就与原版一致**（这 44 张的原版值确实几乎全是 `mip=1 / wrap 1,1 / filter 1 / aniso 1`）、
**1 张改**：`ui/40k_voicelines_radio_wave_equalizer.png.meta` 的 `wrapU: 1 → 0`（原版是 `(0,1)` 混合档）。
`typecheck` **0 / 0**；直跑 `csc @wf_csc_editor.rsp` 后 `grep "ArtBaker.cs.*warning"` = **0 条**（W50 踩过的那个坑，已验）。

## 1 · 代码改动（`ArtBaker.cs`，2 处，+22 行）

| # | 位置（现读行号） | 改了什么 |
|---|---|---|
| ① | `:867-882`（`TryFindTruth`） | 候选循环改成两段：原来是 `foreach (n) if (t.TryGetValue(n)) return true;`，现在**先查精确字典、再查规范名索引**（`canon.TryGetValue(CanonName(n), out row)`）。顺序 = 原样 → 剥后缀 → 空格形态 → 规范名，**精确名永远排最前** |
| ② | `:884-897`（新增 `ArtTruthCanonical()` + `_artTruthCanon`） | `_artTruth` 的规范名索引，**首次查表时才建**，字典用 `StringComparer.OrdinalIgnoreCase` —— 与 `CompTruthsCanonical()` 同一套做法（⛔ 不写成 `static readonly`，静态字段按文本顺序初始化会拿到 null） |

- `CanonName()` 是 W50 已在 `:1134` 加好的那份，**没有新建第二份**（避免两处同义实现）。
- 文件 **1193 → 1215 行（+22）**；二进制现读 `CRLF 1215 / LF 1215 / 裸 CR 0` ⇒ **纯 CRLF 保持**。
- **账的落点不受影响**：`grep -rn "ArtBaker" Unity/账/` = 0 命中（W50 已核，本包复核仍为 0）。
- ✅ 标记已打在 `:872`：`// ✅ 已复刻（2026-11-04 · B141 · W61 · 判据 = …）`。
- 🔑 **为什么这不可能造成回归**：旧的三档**先跑、命中就 return**，规范名这一档只在**旧三档全落空后**才查 ⇒ 任何原本查得到的贴图**结果一模一样**。

## 2 · 44 张逐张对照（原版真值 vs 盘上 `.meta`）

口径（镜像 `ArtBaker`，⛔ 不复制第二份名单）：
- 真值 = `vfx_texture_mips.tsv` / `arena_texture_import.tsv` **按表头认列**，arena 覆盖 vfx（`LoadArtTruth` 的口径）。
- 值序 = `mipCount / wrapU / wrapV / filterMode / aniso`；`.meta` 侧对应 `enableMipMap / wrapU / wrapV / filterMode / aniso`。
- 「改前」= 本包动手**之前**从盘上读到的值；「改后」= 现在盘上的值。

| # | 文件 | 命中的原版名 | 原版真值 | 判据 | 改前 | 改后 |
|---|---|---|---|---|---|---|
| 1 | `40k_Rewards_icon_base_card_SDF.png.meta` | `40k_Rewards_icon_base card SDF` | 1/1/1/1/1 | `vfx_texture_mips.tsv:297` | 0/1/1/1/1 | 0/1/1/1/1（已一致） |
| 2 | `40k_voicelines_radio_wave_equalizer.png.meta` | `40k_voicelines_radio_wave equalizer` | 1/0/1/1/1 | `vfx_texture_mips.tsv:319` | 0/**1**/1/1/1 | 0/**0**/1/1/1 ✅ 改 |
| 3 | `40k_Generic_Smooth_line.png.meta` | `40k_Generic Smooth line` | 1/1/1/1/1 | `vfx_texture_mips.tsv:296` | 0/1/1/1/1 | 已一致 |
| 4 | `40k_battle_Win_Skull_SDF.png.meta` | `40k_battle_Win Skull SDF` | 1/1/1/1/1 | `vfx_texture_mips.tsv:300` | 0/1/1/1/1 | 已一致 |
| 5 | `WF_icon_clock_for_shader.png.meta` | `WF_icon_clock for shader` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2896` | 0/1/1/1/1 | 已一致 |
| 6 | `40K_shop_offer_booster_Space_Wolves.png.meta` | `40K_shop_offer_booster_Space Wolves` | 1/1/1/1/1 | `vfx_texture_mips.tsv:61` | 0/1/1/1/1 | 已一致 |
| 7 | `UI_Deck_Master_Zacharial.png.meta` | `UI_Deck_Master Zacharial` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2780` | 0/1/1/1/1 | 已一致 |
| 8 | `UI_Deck_Warlord_Acolyte_Iconward.png.meta` | `UI_Deck_Warlord_Acolyte Iconward` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2783` | 0/1/1/1/1 | 已一致 |
| 9 | `UI_Deck_Warlord_Anvirr_Keltoc.png.meta` | `UI_Deck_Warlord_Anvirr Keltoc` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2784` | 0/1/1/1/1 | 已一致 |
| 10 | `UI_Deck_Warlord_Beastboss_Morgrim.png.meta` | `UI_Deck_Warlord_Beastboss Morgrim` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2789` | 0/1/1/1/1 | 已一致 |
| 11 | `UI_Deck_Warlord_Boss_Zagstruk.png.meta` | `UI_Deck_Warlord_Boss Zagstruk` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2791` | 0/1/1/1/1 | 已一致 |
| 12 | `UI_Deck_Warlord_Celestian_Sacresant_Aveline.png.meta` | `UI_Deck_Warlord_Celestian Sacresant Aveline` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2792` | 0/1/1/1/1 | 已一致 |
| 13 | `UI_Deck_Warlord_Chaplain_Letharius.png.meta` | `UI_Deck_Warlord_Chaplain Letharius` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2793` | 0/1/1/1/1 | 已一致 |
| 14 | `UI_Deck_Warlord_Commander_OMaisos.png.meta` | `UI_Deck_Warlord_Commander OMaisos` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2794` | 0/1/1/1/1 | 已一致 |
| 15 | `UI_Deck_Warlord_Commissar_Deckler.png.meta` | `UI_Deck_Warlord_Commissar Deckler` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2795` | 0/1/1/1/1 | 已一致 |
| 16 | `UI_Deck_Warlord_Daemon_Prince.png.meta` | `UI_Deck_Warlord_Daemon Prince` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2796` | 0/1/1/1/1 | 已一致 |
| 17 | `UI_Deck_Warlord_Eliac_Zephyrblade.png.meta` | `UI_Deck_Warlord_Eliac Zephyrblade` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2797` | 0/1/1/1/1 | 已一致 |
| 18 | `UI_Deck_Warlord_Erika_Luminas.png.meta` | `UI_Deck_Warlord_Erika Luminas` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2798` | 0/1/1/1/1 | 已一致 |
| 19 | `UI_Deck_Warlord_Ghazghkull_Thraka.png.meta` | `UI_Deck_Warlord_Ghazghkull Thraka` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2800` | 0/1/1/1/1 | 已一致 |
| 20 | `UI_Deck_Warlord_Grukk_Face-Rippa.png.meta` | `UI_Deck_Warlord_Grukk Face-Rippa` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2803` | 0/1/1/1/1 | 已一致 |
| 21 | `UI_Deck_Warlord_Haarken_Worldclaimer.png.meta` | `UI_Deck_Warlord_Haarken Worldclaimer` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2805` | 0/1/1/1/1 | 已一致 |
| 22 | `UI_Deck_Warlord_Hektor_Thenmann.png.meta` | `UI_Deck_Warlord_Hektor Thenmann` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2806` | 0/1/1/1/1 | 已一致 |
| 23 | `UI_Deck_Warlord_Imotekh_the_Stormlord.png.meta` | `UI_Deck_Warlord_Imotekh the Stormlord` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2807` | 0/1/1/1/1 | 已一致 |
| 24 | `UI_Deck_Warlord_Jain_Zar.png.meta` | `UI_Deck_Warlord_Jain Zar` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2809` | 0/1/1/1/1 | 已一致 |
| 25 | `UI_Deck_Warlord_Junith_Eruita.png.meta` | `UI_Deck_Warlord_Junith Eruita` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2810` | 0/1/1/1/1 | 已一致 |
| 26 | `UI_Deck_Warlord_Lhaska_Szenari.png.meta` | `UI_Deck_Warlord_Lhaska Szenari` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2812` | 0/1/1/1/1 | 已一致 |
| 27 | `UI_Deck_Warlord_Logan_Grimnar.png.meta` | `UI_Deck_Warlord_Logan Grimnar` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2814` | 0/1/1/1/1 | 已一致 |
| 28 | `UI_Deck_Warlord_Lord_Exultant.png.meta` | `UI_Deck_Warlord_Lord Exultant` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2816` | 0/1/1/1/1 | 已一致 |
| 29 | `UI_Deck_Warlord_Marneus_Calgar.png.meta` | `UI_Deck_Warlord_Marneus Calgar` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2819` | 0/1/1/1/1 | 已一致 |
| 30 | `UI_Deck_Warlord_Medreyal_Ghaelyn.png.meta` | `UI_Deck_Warlord_Medreyal Ghaelyn` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2820` | 0/1/1/1/1 | 已一致 |
| 31 | `UI_Deck_Warlord_Morvenn_Vahl.png.meta` | `UI_Deck_Warlord_Morvenn Vahl` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2821` | 0/1/1/1/1 | 已一致 |
| 32 | `UI_Deck_Warlord_Mozrog_Skragbad.png.meta` | `UI_Deck_Warlord_Mozrog Skragbad` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2822` | 0/1/1/1/1 | 已一致 |
| 33 | `UI_Deck_Warlord_Nemesor_Zahndrekh.png.meta` | `UI_Deck_Warlord_Nemesor Zahndrekh` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2823` | 0/1/1/1/1 | 已一致 |
| 34 | `UI_Deck_Warlord_Njal_Stormcaller.png.meta` | `UI_Deck_Warlord_Njal Stormcaller` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2825` | 0/1/1/1/1 | 已一致 |
| 35 | `UI_Deck_Warlord_Orikan_the_Diviner.png.meta` | `UI_Deck_Warlord_Orikan the Diviner` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2826` | 0/1/1/1/1 | 已一致 |
| 36 | `UI_Deck_Warlord_Ragnar_Blackmane.png.meta` | `UI_Deck_Warlord_Ragnar Blackmane` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2827` | 0/1/1/1/1 | 已一致 |
| 37 | `UI_Deck_Warlord_Saffa_Rhiannor.png.meta` | `UI_Deck_Warlord_Saffa Rhiannor` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2829` | 0/1/1/1/1 | 已一致 |
| 38 | `UI_Deck_Warlord_Sylar_Hexcorn.png.meta` | `UI_Deck_Warlord_Sylar Hexcorn` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2832` | 0/1/1/1/1 | 已一致 |
| 39 | `UI_Deck_Warlord_Tempestor_Prime.png.meta` | `UI_Deck_Warlord_Tempestor Prime` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2833` | 0/1/1/1/1 | 已一致 |
| 40 | `UI_Deck_Warlord_Uriel_Ventris.png.meta` | `UI_Deck_Warlord_Uriel Ventris` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2837` | 0/1/1/1/1 | 已一致 |
| 41 | `UI_Deck_Warlord_Ursula_Creed.png.meta` | `UI_Deck_Warlord_Ursula Creed` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2838` | 0/1/1/1/1 | 已一致 |
| 42 | `UI_Deck_Warlord_Valius_Paxor.png.meta` | `UI_Deck_Warlord_Valius Paxor` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2840` | 0/1/1/1/1 | 已一致 |
| 43 | `UI_Deck_Warlord_Varro_Tigurius.png.meta` | `UI_Deck_Warlord_Varro Tigurius` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2841` | 0/1/1/1/1 | 已一致 |
| 44 | `UI_Deck_Warlord_Winged_Tyranid_Prime.png.meta` | `UI_Deck_Warlord_Winged Tyranid Prime` | 1/1/1/1/1 | `vfx_texture_mips.tsv:2843` | 0/1/1/1/1 | 已一致 |

**分布**：44 张 = `ui/` 1 张 + `ui_menu/` 43 张（其中 37 张 `UI_Deck_Warlord_*` + `UI_Deck_Master_Zacharial` + 5 张杂项）；
真值**全部来自 `vfx_texture_mips.tsv`**（0 张来自 arena 表）；44 张的原版 `mipCount` **全是 1**、`filterMode` 全 1、`aniso` 全 1，
wrap 只有第 2 行那 1 张是 `(0,1)`，其余全 `(1,1)`。
⇒ 这也解释了「为什么只有 1 张要改」：**这 44 张原版值本来就与我们的默认值一致**，真正被漏掉的是「查得到」这件事本身 —— 修掉它才保证**将来有人改了我们的 `.meta` 还会被烘焙纠回原版**（而不是「表外保留现值」）。

## 3 · 验证

### 3·1 `typecheck`

```
$ TMPDIR=/tmp/wf_b141_w61 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

### 3·2 warning（W50 踩过的坑，单独跑）

```
$ cd d:/4/Unity/MyGame && dotnet .../csc.dll @/tmp/wf_b141_w61/wf_csc_editor.rsp
rc=0 · error CS = 0 · grep "ArtBaker.cs.*warning" = 0 条
```
（其余文件共 119 条 warning 与本次改动无关，改动前就在；`ArtBaker.cs` 里一条都没有。）

### 3·3 静态逐张（可复跑：`python -I -X utf8 d:/4/_tmp_view/b141_w61/verify.py`）

- 脚本**先从 `ArtBaker.cs` 源码里正则取出 `CanonName` / `TryFindTruth` / `ArtTruthCanonical` 三段**并断言其形态
  （`CanonName` 必须逐字等于 `s.Replace("_", "").Replace(" ", "")`；`TryFindTruth` 必须含 `canon.TryGetValue(CanonName(n), out row)`；
  索引必须用 `StringComparer.OrdinalIgnoreCase`）⇒ **不是把规则抄第二份**。
- 真值表按表头认列重读、vfx→arena 覆盖顺序与 C# 一致。
- 结果：**44 张（旧三档漏、规范名命中）= 44**；**「`TryFindTruth` 算出的值」vs「盘上 `.meta` 现值」= 0 不符**。
- 同一脚本也复算了旧三档：**93 命中**（= W40/W50 的 `137 - 44`）⇒ 与 W50 报告的数字对得上。

### 3·4 `.meta` 二进制读回

`CardPresentation/Resources/Art/ui/40k_voicelines_radio_wave_equalizer.png.meta`：

| | 改前 | 改后 |
|---|---|---|
| 字节数 | 2652 | **2652**（`1 → 0` 同宽度） |
| 行数 | 117 | **117** |
| 行尾 | 纯 LF（CRLF 0 / 裸 CR 0） | **纯 LF（CRLF 0 / 裸 CR 0）** |
| 内容差异 | — | 仅 `textureSettings.wrapU: 1` → `0`（`wrapV/wrapW` 未动；真值不含 W，见 `ArtBaker.cs:54`） |

⚠️ `Resources/Art/` 整棵子树在 `.gitignore:124` 里 ⇒ 这些 `.meta` **不进 git**，`git status` 看不到；
所以本包的核验一律走**盘上读回**，不依赖 `git diff`。

## 4 · 顺手发现（**未改**，白名单之外）

1. 🔴 **代码改完后，下一次 `ArtBaker` 烘焙会顺带改 3 张 `card3d` 的 `.meta`**（本包**没动**它们，白名单只到那 44 张）：
   `Resources/Art/card3d/MatCap_Bronze.png.meta` / `MatCap_Gold.png.meta` / `MatCap_Silver.png.meta`
   —— 原版名 `Matcap Bronze` / `Matcap Gold` / `Matcap Silver`（`vfx_texture_mips.tsv:1909/1910/1913`），`mipCount = 8`
   ⇒ 规范名一接上，`OriginalMipEnabled` 会返回 `true` ⇒ **`enableMipMap: 0 → 1`**（wrap 0,0 / filter 1 / aniso 1 已一致）。
   按铁律 11（照原版）应当开；但这是**画面可见**的改动（MatCap 缩小采样会更平滑），且不在本包白名单 ⇒ **请调度台拍一句**。
   ⚠️ 现状：**代码已经会这么做了**，只是要等下次烘焙才落盘 —— 也就是说「不拍就等于默认照原版开」。
2. 全树（`Resources/Art`，3335 个 `png.meta`）里，**只有规范名这一档能命中**的共 **85 张**
   （`ui_menu` 57 · `boosterpack` 21 · `card3d` 5 · `ui` 2）；其中**与现值不同**的只有 **4 张**：上述 3 张 + 本包改的那 1 张。
   ⇒ 余下 81 张本来就一致，**不需要动**。
3. 真值表内存在 **1 对** `CanonName` 碰撞：`SM_SpaceWolves_inf_ Iron Priest` 与 `SM_SpaceWolves_inf_Iron Priest`
   （同一份 `vfx_texture_mips.tsv`，**五个值逐列相同**）⇒ 索引「后写覆盖」也无歧义后果。
   （W50 报告的「144 条 CompTruths 之间 0 碰撞」是**另一张表**，两者不矛盾。）
4. `IsOriginalNormalMap` 仍是**旧三档**（没有接 canon）；实测 `Resources/Art` 下**0 个文件**会因 canon 而新判为法线图
   ⇒ 现在不动是安全的（且在白名单外）。
5. `OriginalNameCandidates` 会剥 `_0` / `_1` 后缀；这 44 张的落盘名都不带这两个后缀，**剥后缀档对它们不起作用**，
   命中的确实是「空格形态 / 规范名」这两档。

## 5 · ❓

- **没有查不到的**：44 张全部命中，0 张需要「表外保留现值」。
- **待调度台定 1 条**：§4·1 那 3 张 `MatCap_*` 要不要**现在**就按原版开 mip（否则等下次烘焙，效果一样、只是时间点不同）。
- 建议顺手核销：W50 报告 §5·1 这条「已修」；W40 报告 §6 里那句「`OriginalCompression` 已经会按『原版名 / 下划线形态』查，无需再改查表逻辑」在两处都被证明不成立（W50 修压缩档、本包修 mip/wrap/filter/aniso）。

## 6 · 产出物（临时目录，只读可复跑）

| 文件 | 用途 |
|---|---|
| `d:/4/_tmp_view/b141_w61/analyze.py` | 三档 vs 三档+规范名的命中对比（复现 W50 的 93 / 44） |
| `d:/4/_tmp_view/b141_w61/diff.py` | 44 张真值 vs `.meta` 现值 |
| `d:/4/_tmp_view/b141_w61/tree_scan.py` | 全树 3335 张扫「仅规范名命中」（85 张 / 4 张有差异） |
| `d:/4/_tmp_view/b141_w61/verify.py` | **正式校验**：先断言 C# 代码形态，再算 44 张（断言 44 / 0 不符） |
| `d:/4/_tmp_view/b141_w61/table44.md` · `new_only.json` · `tree_newly.json` | 中间产物 |
