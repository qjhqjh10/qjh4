# B9_W30ALTART —— 异画贴图链（7 张里 1 张恒 null）

> 2026-10-27 · 执行代理 `B9_W30ALTART` · 白名单：`工具/import_original_art.py` · `Resources/Art/altarts/*`（含 `.meta`，生成物）

## 一、结论

1. **根因不是「取图规则写错」，而是「id 改过名、脚本没重跑」留下的旧名产物。**
   `工具/import_original_art.py` 的 `ALT_ART` 表里 DA1 那一行的 id 换过三次
   （git 实测：`DA3` → `DA_Azrael`（2026-10-14 16:26 提交 `843e78a4`）→ `DA1`（2026-10-15 02:24 提交 `ee784b5e`）），
   而盘上那批 PNG 是 **2026-10-14 15:25** 生成的（当时 id = `DA_Azrael`，文件名 `alt_da_azrael.png` **在当时是对的**）。
   id 改成 `DA1` 之后**从没重跑过 `--only-altart`** ⇒ 盘上留着旧名，
   而 `CardArt.AltArt("DA1")` = `Resources.Load("Art/altarts/alt_" + Slug("DA1"))` = **`alt_da1`** ⇒ **恒 null**。
   （`.meta` 的 mtime 是 2026-10-18 10:29 —— Unity 给这 7 张补过 meta，**没有暴露**文件名和规则不一致，因为它只认盘上叫什么。）

2. **修复 = 重跑生成器 + 删旧名残件 + 保住 guid。**
   `alt_da1.png` 由 `--only-altart` 重生成，**像素与旧 `alt_da_azrael.png` 逐字节相同**
   （sha256 `172194a598d1d1631392611becfcf30e807c859cdb234edaa2744940eb4299fe`，928,712 B，671×1024），
   旧件的 `.meta`（guid `7027f20be00a3c24fafe80161b33385d`）**原样拷到 `alt_da1.png.meta`** 后再删旧件
   ⇒ 即使将来有序列化引用也不会断（本轮实测：全 `Assets` 对那个 guid / 旧名 `alt_da_azrael` **零引用**）。
   其余 6 张重生成后 **sha256 与改前完全一致** ⇒ 这条管线是确定性、可复现的。

3. **规则收敛成一处（消除了一个潜在分叉）。**
   导入器原来写 `f'alt_{cid.lower()}.png'`，而 C# 侧走 `CardArt.Slug`（非字母数字 → `_`）。
   对当前 7 个纯字母数字 id 两者结果相同，但对带 `-`/`.` 的 id 会**静默分叉**。
   现在改成本文件已有的 `slug(cid)`（它的 docstring 就写着「和 C# 的 `CardArt.Slug` 保持一致」）⇒ 只有一个归一化实现。

4. **自检加固：`--only-altart --check` 现在连【产物】一起查。**
   原来 `--check` 只查「源在不在」—— 正是这个缺口让本条静默了 13 天（源一直在，缺的是产物名对不上）。
   修前现读：`检查完成：6 / 7` + `🔴 缺产物 … alt_da1.png`；修后：`检查完成：7 / 7`。

5. **取图真源确认**：`CardArt.AltArt` 读的是 **PNG**（`Resources.Load<Texture2D>`，`Core/CardArt.cs:1035-1039` / `:1060-1068`）。
   `alt_cutouts.json` **不是贴图源** —— 它只被 `AltArtHasCutout` 读成 `TextAsset`（`:1044-1057`），
   决定卡面「画不画越出卡框的前景层」。两者必须同名同 id，本轮一并重生成（`alt_da_azrael` → `alt_da1`）。

6. **幂等**：连跑两遍 `--only-altart`，7 张 PNG + `alt_cutouts.json` 共 8 个产物 **sha256 完全一致**（逐字节稳定）。

### 7 张异画 × 文件名 × 存在性（现读，`cards_engine.json` 非空 `alternateArts` = 7 张）

| 卡 id | 卡名 | alternateArts | `CardArt.AltArt(id)` 取的文件 | 修前磁盘 | 修后磁盘 | 导入器表 | 抠图清单 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| AM2 | Ursula Creed | `#HB_AM2` | `alt_am2.png` | ✅ | ✅ 671×1024 | `import_original_art.py:1043` | ✅ |
| BL34 | Abaddon the Despoiler | `#HB_BL34` | `alt_bl34.png` | ✅ | ✅ 671×1024 | `:1044` | ✅ |
| **DA1** | **Azrael** | `#HB_DA1` | **`alt_da1.png`** | ❌ **只有 `alt_da_azrael.png`（规则对不上 ⇒ null）** | ✅ 671×1024 | **`:1045`** | ✅（原记 `alt_da_azrael`） |
| SAU35 | Imotekh the Stormlord | `#HB_SAU35` | `alt_sau35.png` | ✅ | ✅ 671×1024 | `:1046` | ✅ |
| GOF28 | Ghazghkull Thraka | `#HB_GOF28` | `alt_gof28.png` | ✅ | ✅ 671×1024 | `:1047` | ✅ |
| SW1 | Logan Grimnar | `#HB_SW1` | `alt_sw1.png` | ✅ | ✅ 671×1024 | `:1048` | ✅ |
| UM76 | Lieutenant Titus | `#HB_UM76` | `alt_um76.png` | ✅ | ✅ 671×1024 | `:1049` | ✅ |

（7 张源图全部存在：`d:/2/新解包资源/assets_full/bundle_*cardassets_assets_all/Texture2D/*.png` 各 1024²；
裁法 = `Sprite/<同名>.json` 的 `textureRect` ⇒ 输出 671×1024，与其余 6 张同口径。）

## 二、证据（文件:行号）

- `MyGame/Assets/CardPresentation/Core/CardArt.cs:1034-1039` —— `AltArt(cardId)` = `Get(Root + "altarts/alt_" + Slug(cardId))`；`Root = "Art/"`（`:18`）。
- `MyGame/Assets/CardPresentation/Core/CardArt.cs:703-710` —— `Slug`：`ToLowerInvariant` + 非字母数字 → `_` ⇒ `"DA1"` → `"da1"`。
- `MyGame/Assets/CardPresentation/Core/CardArt.cs:1060-1068` —— `Get` = `Resources.Load<Texture2D>(path)`，**null 也缓存**（所以缺图是静默的、不会每次报）。
- `MyGame/Assets/CardPresentation/Core/CardArt.cs:1044-1057` —— `AltArtHasCutout` 读 `Resources.Load<TextAsset>("Art/altarts/alt_cutouts")`（= json 只作抠图清单）。
- `工具/import_original_art.py:1030-1039` —— 命名坑与「规则只有一处 / 别再写 `.lower()`」的说明（本轮新增第 1034-1039 行）。
- `工具/import_original_art.py:1043-1049` —— `ALT_ART` 7 行（DA1 在 `:1045`，源 `DarkAngels_AA_warlord_Azrael_v2.png`）。
- `工具/import_original_art.py:1053-1070` —— `altart_jobs()`：源存在性 + **`f'alt_{slug(cid)}.png'`**（本轮改的一行在 `:1069`）。
- `工具/import_original_art.py:1705-1742` —— `--only-altart` 分支：`--check` 现在查产物存在性（`:1714-1721`），缺产物单列打印（`:1739`）；清单 note 文案订正（`:1732`）。
- `MyGame/Assets/RuleEngine/Resources/cards_engine.json` —— 现读 1128 张卡，`alternateArts` 非空 **7 张**（`#HB_<卡id>`）。
- `d:/4/.gitignore:93` —— `/Unity/MyGame/Assets/CardPresentation/Resources/Art/` 整目录忽略（**既定设计，未改**）。
- 消费者（只读核对，未改）：`Battle/BattleDriver.cs:34537-34543`（`AltArtOverrideOf` → `CardArt.AltArt(c.Id)`，取不到时 `LogWarning`）；
  `Shell/CardDetailPopup.cs:1749`（`CardArt.AltArt(Card.Id)`）；`Shell/CollectionWindow.cs:2635`（`d.artOverride = CardArt.AltArt(a.CardId)`，收藏/异画页）；
  `Battle/BattleDriver.cs:34547-34570`（`CollectAltArtCopies`：`#HB_SW1` → 取最后一个 `_` 之后那段 → `SW1`）。
- git 证据（改名时间线）：`843e78a4`（2026-10-14 16:26）仍是 `DA_Azrael`；`ee784b5e`（2026-10-15 02:24）已是 `DA1`；
  盘上 PNG 的 mtime = 2026-10-14 15:25 ⇒ **生成早于改名，之后没重跑**。

## 三、没查清的部分

1. **没有运行时证据**：本波红线「不跑 Unity」⇒ `CardArt.AltArt("DA1") != null` 只有**静态链**（path / 文件 / meta 全对），没有实拍或断言。
   恢复 Unity 后的最小验收：在任一宿主里 `Check(CardArt.AltArt("DA1") != null)`，并把它与另 6 张一起放进同一批。
2. **没有 player 包证据**：`Resources/Art/` 会在构建时被打进包，但**没验** player 里这 7 张的压缩/尺寸（本轮不跑构建）。
3. **其它生成物是否也有「id 改过没重跑」的同族问题**：本轮只核异画 7 张；全量生成物（`art_*` / `frame_*` / `back_*` 等）的对账不在本件范围，**没有查**。
4. **`Resources/Art/altarts/` 之外是否还有第二处异画名清单**：全 `Assets` grep `altarts` 只命中 `CardArt` / `CardView`（注释）/ `CardDetailPopup`（注释）/ `BattleDriver`（日志串），**没有发现第二份硬编码名单**；但 `Editor/CollectionScene.cs:9973` 用 `mainTexture.name.StartsWith("alt_")` 间接依赖文件名前缀（重生成后仍成立）。

## 四、改动清单

1. `工具/import_original_art.py`（`git diff --numstat` = 27/8；行尾保持 **CRLF**，实测 `CRLF 2000 / LF 2000 / 裸 CR 0`）
   - `:1031` 头注：「`alt_<卡 id 小写>`」→「`alt_<卡 id>`」
   - `:1034-1039` 新增：**命名规则只有一处**（用 `slug()`，⛔ 别写 `.lower()`）+ 本次事故的根因与时间线 + `--check` 会点名缺产物
   - `:1053-1056` `altart_jobs()` docstring 补一句「文件名用 `slug(cid)`」
   - `:1069` `f'alt_{cid.lower()}.png'` → **`f'alt_{slug(cid)}.png'`**（唯一的实质逻辑改动）
   - `:1714-1721` `--only-altart --check` 新增**产物存在性**检查（`lack` 列表）
   - `:1732` `alt_cutouts.json` 的 note 文案：「`alt_<卡 id 小写>`」→「`alt_<卡 id>`，id 走 slug 归一化」
   - `:1738-1739` 缺源 / 缺产物**分开打印**（`缺源:` / `🔴 缺产物（取图规则对不上，重跑一次 --only-altart）:`）
   - `:1741-1742` 订正那句提示：`ArtBaker.ApplyImportSettings` 的作用是「统一压缩/关 NPOT 缩放」，**异画不需要 Read/Write**（只有 `cards/frame_*` 要，`Editor/ArtBaker.cs:443/476`）
2. `MyGame/Assets/CardPresentation/Resources/Art/altarts/`（被 gitignore 的生成物）
   - 新增 `alt_da1.png`（= 旧 `alt_da_azrael.png` 逐字节相同）+ `alt_da1.png.meta`（从旧 meta **原样拷贝**，guid `7027f20b…`，与另 6 份设置逐字节相同、仅 guid 不同）
   - 删除 `alt_da_azrael.png` 与 `alt_da_azrael.png.meta`（零引用，见上）
   - 重生成 `alt_cutouts.json`：`cards` 里 `alt_da_azrael` → **`alt_da1`**（`count` 仍 7），note 文案同步
   - 其余 6 张 PNG 重生成后 **sha256 与改前完全一致**（`alt_am2`/`alt_bl34`/`alt_gof28`/`alt_sau35`/`alt_sw1`/`alt_um76`）
3. `资料/完全复刻/账/_parts/B9_W30ALTART.tsv`（11 列 · 首行表头 · 单行补丁）
   - 更新 `战斗界面_HUD.tsv:92`（`BattleCardManager.CheckToBeAlternate(RawCardScript rawCardData, bool isPlayer)`）：
     `差异` 列把「DA1 恒 null」的告警改成「✅ 2026-10-27 已修 + 根因」；`备注` 列追加本轮结论。
   - 跑过 `python -I -X utf8 工具/check_patch_conflicts.py` ⇒ `✅ 没有跨补丁重复键（4 份补丁）`。

## 五、顺手发现

1. 🔴 **`Battle/BattleDriver.cs:34541` 的缺图日志串是错的**：拼的是 `"Art/altarts/alt_" + c.Id`（大写原样 ⇒ `alt_DA1`），
   而真实路径是 `alt_da1`（`Slug` 会小写）。**只影响排查**（照它去磁盘找永远找不到），不影响取图。
   该文件归 `W29`，⛔ 未改 —— **改动请求**：改成 `CardArt.Slug(c.Id)` 或直接引 `CardArt` 的口径。
2. `alt_cutouts.json` 的 note 原来写「文件名 = alt_<卡 id **小写**>」，与真实规则（slug）不符 —— 本轮一并订正（在生成器里，不手改产物）。
3. 导入器所有模式结尾那句「导完记得跑 `ArtBaker.ApplyImportSettings`（否则新 PNG 没开 Read/Write）」其实**只对卡框成立**
   （`Editor/ArtBaker.cs:443` `isFrame = path.Contains("/cards/frame_")`、`:476` `ti.isReadable = isFrame`；7 张异画 meta 实测全 `isReadable: 0`）。
   异画分支的提示已订正；**其它模式的同一句（`:1695`）未动**（不在本件判据内，且对 `cards/frame_*` 仍是对的）。
4. `--check` 的「只查源」是**这一类缺陷的共性缺口**：`源在、产物旧名` 既不报错也不缺源 ⇒ 看起来 7/7 全绿。
   本轮只补了 `--only-altart` 一支；其它模式（`portrait_jobs` / 卡背 / 菜单图）的 `--check` 是否也只查源 —— **没查**（建议另开一件）。
