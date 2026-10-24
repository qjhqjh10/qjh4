# D4 · 两条 `ev=deploy` 的 `orphan` 行（生成侧处置）

> 只改了白名单里的两个文件：`工具/import_original_audio.py` · `MyGame/Assets/CardPresentation/Resources/voice_lines.json`（+ 本报告）。
> ⛔ 未跑 Unity · 未动 git · 未改任何正本 · ⛔ 没碰 `Core/VoiceLines.cs`（B4 的「SO 优先」原样保留）。
> 来源：`资料/普查产出_1024/B4_语音兜底两处.md` §七·4。

---

## 〇、一句话结论

两条**都剔掉了**（生成侧，⛔ 不是运行期）：

| 卡 | 行 | 来源级 | 处置 |
|---|---|---|---|
| `AM70` Scion Medic | `VO_AM_Medic Scion - No soul left behind.ogg` · `deploy` · `src=orphan` | 生成器 **③ 按文件名猜**（`match_owner` 短名 `scion`） | **剔除**（同卡同事件已有卡 SO 指名，本行在 1,351 张 SO 里**零引用**） |
| `UM21` Primaris Judiciar | `VO_NOT USED_UM_Primaris Judiciar - You're not ready to take on a foe like me.ogg` · `deploy` · `src=orphan` | 生成器 **③ 按文件名猜**（`match_owner` 全名 `primaris judiciar`） | **剔除**（文件名自带原版的 `VO_NOT USED_` 标记，且零 SO 引用） |

**新计数：642 卡 / 1,892 条**（原 1,894）· **新 sha256 `fce7e91e8e4e92a24061047911e0784dde213412d3a3840e398fb29a292bbf7a`** ·
连跑两次同 sha · `--check` rc=0。

---

## 一、这两条是从哪来的（来源链，逐级排掉）

生成器 `工具/import_original_audio.py` 的归属是**四级**（文件头 `:34-42`）：① 卡 SO 的三个战斗字段 → ①·B 卡 SO 的 12 个 `*ChatSound` → ② `card_index.json` 兜底 → ③ **按文件名猜**（`src=orphan`）。
**这两条都是 ③ 的产物**（不是目录扫描 —— 目录扫描只用来列资产，归属一律靠名字）。

### 1·1 `AM70` …`No soul left behind`

| 级 | 结果 | 判据 |
|---|---|---|
| ① 卡 SO 三字段 | ❌ 不认 | AM70 的 SO `summonAudio` 指的是**另一条**：`VO_AM_Medic Scion -On your feet! If you can breathe you can fight` |
| ①·B 12 个 chat 字段 | ❌ 全空 | SO 里 `defaultChatSound` / `introChatSound` / … 12 个字段 `m_AssetGUID` 全是空串 |
| ② `card_index` | ⚠️ **收录了、但 join 不上** | `card_index.json` 里它挂在 `('AstraMilitarum', 'Medic Scion')` 下；我们卡表叫 **`Scion Medic`**（词序相反）⇒ `ekey` 查不到 ⇒ 该文件**没被 ② 认走** |
| ③ 按文件名猜 | ✅ **这条就是这样进来的** | `match_owner`：全名 `scion medic` 不在 header `vo am medic scion` 里；**首段短名 `scion`（≥5 字符、池里唯一）命中** ⇒ 记到 AM70，`src=orphan` |

> 词序这条有出处：`工具/gen_cards_engine.py:242` —— `"Medic Scion": "rare",  # …（卡面印 Scion Medic）`
> ⇒ 卡名是**卡面口径** `Scion Medic`，`card_index.json` 用的是**本地化键** `Medic Scion`。

### 1·2 `UM21` …`VO_NOT USED_…`

| 级 | 结果 | 判据 |
|---|---|---|
| ① 卡 SO 三字段 | ❌ 不认 | UM21 的 SO `summonAudio` 指 `VO_UM_Primaris Judiciar - Intro` |
| ①·B 12 个 chat 字段 | ❌ 全空 | 同 AM70，12 个字段全空串 |
| ② `card_index` | ❌ **根本没收录** | 逐条扫 `card_index.json` 的 `cards[].voice`：**零命中**（只有 AM70 那条在这份索引里） |
| ③ 按文件名猜 | ✅ **这条就是这样进来的** | `match_owner`：header `vo not used um primaris judiciar` 含**归一化全名** `primaris judiciar` ⇒ 记到 UM21，`src=orphan` |

---

## 二、SO 现读（当前 build 1,351 张 `RawCardScript`）

源：`资料/普查产出_1018/allcards_1350/cards_raw.json`（`full_raw`），逐字段 `m_AssetGUID` 现读。

| 卡 | SO `uniqueId` | `cardName` | `summonAudio` | 解出的 clip | 其余 14 个音频字段 |
|---|---|---|---|---|---|
| `AM70` | `AM70` | Medic Scion | `480f84a1c41d6554ebc94549dd918854` | `VO_AM_Medic Scion -On your feet! If you can breathe you can fight` | **全空** |
| `UM21` | `UM21` | Primaris Judiciar | `7f4de0ce08d674595b4a29a38d1e7710` | `VO_UM_Primaris Judiciar - Intro` | **全空** |

**这两条 clip 在全部 1,351 张 SO 的 15 个音频字段里都是零引用**（2026-10-24 现读）：

- 扫面：1,351 × 15 个字段 ⇒ 1,315 个**不同** clip 名；两条目标名**一次都没出现**；
  连「包含 `No soul left behind` / `not ready to take on` 的名字」也**一个都没有**。
- 只有 **3 条** GUID 解不出（已知、与本批无关）：`BL47` summon `fda6e5bc…` · `GOF2`/`77a72532…` death `11641bc6…`。
- GUID→名 的并集与生成器同源：`card_vfx_by_card.json._assetRefs` ∪ `guid_map_bundles.tsv`（仅 AudioClip）∪ `anim_address_map.json`。

**原版取音链的旁证（`d:/2/tools/decomp_full/`）**：

- `RawCardScript__GetSummonSound.c` —— 只读**一个**字段：`*(param_1 + 0xb8)`（`dump.cs:23270` `summonAudio; // 0xB8`），
  引用无效时 `return 0`。**没有第二个候选、没有回落列表**。
- `VoiceLinesController__DisplaySummonMessage.c:31-40` —— 拿 `GetSummonSound` 的返回值 + `GetLocalizedSummonChat`，
  **不按文件名挑 clip**。
  ⇒ 登场台词在原版是「**一个字段 → 一条 clip**」，多出来的同名猜行**没有任何播放路径**。

---

## 三、处置与理由

### 3·1 实现（`工具/import_original_audio.py`）

新增 `orphan_is_dead(base, cid, ev, lines)`（`:300`，docstring 里带全部判据与实测），
**只在 ③ 这一级**调用（`:745`），命中就**不写进表**并**逐条打印**：

```text
🗑 丢弃孤儿行（原版不会播）：VO_AM_Medic Scion - No soul left behind.ogg → AM70 [deploy] · 同卡同事件已有卡 SO 指名的行（本行在全部卡 SO 里零引用）
🗑 丢弃孤儿行（原版不会播）：VO_NOT USED_UM_Primaris Judiciar - You're not ready to take on a foe like me.ogg → UM21 [deploy] · 文件名带 `VO_NOT USED_`（原版自己标了废弃）
🔴 孤儿里**原版不会播、已丢弃**：2 条
```

两条规则（**缺一不可地只对这两种情形生效**）：

1. **文件名自带 `VO_NOT USED_`** —— 原版自己标了废弃。
   全库实测**只有 1 条**（`bundle_spacemarinesultramarinescardassets_assets_all/AudioClip/`）。
2. **同一张卡、同一个事件已经有卡 SO 指名的行**，且**不是对手台词**（文件名不带 `_vs_`），
   而这行在**全部 1,351 张 SO 里零引用** ⇒ 原版不会播它。
   现读只命中 `AM70` 那一条。

⛔ **没有推广成「孤儿全剔」**（这是本条最容易做错的地方）：

| 情形 | 例 | 处置 | 为什么 |
|---|---|---|---|
| `_vs_` 对手台词 | `BL35` 的 4 条 `vs*` · `UM48` 的 `Hexscorn_vs_Varro` | **保留** | 没有 SO 真值可依；B4 的 ⑤e 就是拿它们钉「SO 优先后随机池没被一刀切」 |
| 同事件**一条 SO 行都没有**的组 | `ASH3` 3 条 / `SAU36` 4 条 vs 台词 | **保留** | 同上的设计内兜底 |
| 其余 64 条孤儿 | gen4..7 / backup / tbc … | **保留** | 同上 |

**只在 ③ 生效的副作用（这是有意为之）**：①② 级的行**永远不会**被这条规则碰到 —— 卡 SO 说了算，
哪怕文件名带 `VO_NOT USED_` 也照挂（现读没有这种冲突）。

### 3·2 为什么不是「在 `VoiceLines.cs` 里做」

任务要求保持 B4 的 `src=orphan` 优先规则原样。**没动**。
副作用只有一个：这两条被剔掉之后，运行期 `WarnSuppressed` 对它们**不再报「猜的行被压掉」**
（本来也播不到，行为不变、只是少两条告警）。

### 3·3 幂等与格式

- 连跑两次 sha256 相同（见 §四）；`--check` rc=0。
- 行尾：JSON **纯 LF**（改后 0 CRLF / 16,714 LF）· `.py` **纯 LF**（0 / 870）。
- 没删任何音频：两条 clip 仍在 `MyGame/Assets/CardPresentation/Resources/Art/audio/vo/`（无引用；该目录被 `.gitignore` 排除）。

---

## 四、新计数 / 新 sha256 / `--check` 读数

| 项 | 改前 | 改后 |
|---|---|---|
| 卡 | 642 | **642** |
| 台词总数 | 1,894 | **1,892** |
| `src=cardSO` | 1,370 | **1,370** |
| `src=orphan` | 68 | **66** |
| 无 `src`（② 兜底） | 456 | **456** |
| `deploy` 事件条数 | 606 | **604** |
| `voice_lines.json` sha256 | `16a977df0288679059729f2fa50606ebfe75afc0e8c648eaaf4940da44d0000d` | **`fce7e91e8e4e92a24061047911e0784dde213412d3a3840e398fb29a292bbf7a`** |

- **幂等**：`python -I -X utf8 工具/import_original_audio.py` 连跑两次 ⇒ 两次 sha 都是 `fce7e91e…`。
- `--check`：**rc=0**，打印 `卡片：有语音 642 张 · 台词 1892 条（其中 归属① 1370 条 · 归属③按名字猜 66 条）`。
- `git diff --numstat`（vs HEAD）：JSON **732/402**（改前 731/389 ⇒ 本次只 +1/−13 行：
  `note` 换 1 行 + 两条各 6~7 行删除）· `.py` **236/117**。行尾没翻。
  ⚠️ JSON 的 diff 里还有大头是 **B2 上一轮未提交**的改动（`so` 列 / `owner` 换源），不是本轮引入的 ——
  逐行对过备份：本轮只动了 `note` 那一行 + 删除两条行。

改后逐卡抽查：`AM70` / `UM21` 各只剩 1 条 `deploy`（`src=cardSO`, `so=summonAudio`）· `BL35` 的 4 条 `vs*` 原样 · `UM48` 的 `Hexscorn_vs_Varro` 原样。

---

## 五、没查清 / 没做

1. **`d:/4/Unity/数据/游戏数据/voice_lines.json` 不存在** —— 白名单第 2 条无从改。
   实际只有 `MyGame/Assets/CardPresentation/Resources/voice_lines.json` 这一份，生成器的 `DST_JSON` 写的也是它（唯一正本）。
2. **没删** `Resources/Art/audio/vo/` 下那两条音频（不在白名单；目录被 gitignore）。它们现在**无引用**。
3. **没跑 Unity / `AnimFXCheck.Run`**（红线）。核过现有断言：**没有**针对 `AM70`/`UM21` 行数的断言；
   `语音表卡数 ≥ 600` 仍然成立（642）。B4 期望的 `ANIMFX === 合计：148 通过 / 0 失败 ===` 由同步点跑。
4. **没验证真机「登场时听不到」** —— 需要进游戏。
5. **原版「vs 台词」走哪条通道没查清** —— 只查了 `GetSummonSound`（单一字段）与 `DisplaySummonMessage`；
   那批 `_vs_` 文件在离线包里的播放触发点**没找到判据**（见顺手发现 1）。
6. 没有穷尽「这两条 clip 是否被**非 SO** 的资产引用」（如动画/事件表）—— 本任务的判据是「卡 SO 里空」，已满足。

---

## 六、顺手发现（只报不改，除注明外）

1. 🔴 **`VO_BlackLegion_Hexscorn_vs_Varro.ogg` 挂错了卡** —— 现在挂在 **`UM48` Varro Tigurius** 名下
   （`deploy` · `src=orphan`），但它其实是 **`BL36` Sylar Hexscorn 对 Varro 的台词**：
   - 主人是 Hexscorn：`VO_BlackLegion_Hexscorn_*` 全套（`BL36` 自己 14 条 SO 行俱全，**没有 deploy 行**）。
   - 而 `UM48` 的 SO **`summonAudio` 是空的** ⇒ 我们的表会在 Varro 登场时播一条黑军团台词，**原版这里没有登场语音**。
   - 根因：③ 的 `match_owner` 拿**整条 header** 比卡名，`VO_BlackLegion_Hexscorn_vs_Varro` 里命中的是**对手**
     `varro`（`Varro Tigurius` 的短名，池里唯一），而主人 `hexscorn` 对应的卡名是 `Sylar Hexscorn`
     （全名/首段 `sylar` 都不在 header 里）⇒ 配到对手头上。
   - 同类 `_vs_` 行共 **12 条**被记成 `ev=deploy`（`ASH3`×3 · `BL35`×4 · `SAU36`×4 · `UM48`×1）；
     前三个来自 `card_index`（主人正确），**只有 `UM48` 这条是猜的、主人错**。
   - 建议单独立账：③ 对 `_vs_` 只认 `_vs_` **之前**那一段（主人侧），或干脆不猜。⛔ 本轮没动（超出这 2 条的授权，
     且 B4 的 ⑤e 把 vs 组当夹具）。
2. **`card_index.json` 的卡名与我们卡表的词序不一致** ⇒ ② 级 join 不上，AM70 正是因此掉进 ③。
   `Medic Scion` vs `Scion Medic`（`gen_cards_engine.py:242` 明写卡面是后者）；同族 `Shock Trooper Sgt` / `Leman Russ` /
   `Commissar Elan` 也在生成器打印的 **38 张 `no_join`** 里。`load_engine_name_alias` 只收 `STAT_FIXES` 的**改名**，
   **不处理词序颠倒** ⇒ 这一族的语音归属始终只能靠 ③ 猜。
3. ✅ 已就地同步：生成器文件头（`:36-41`）、输出 JSON 的 `note`、以及 `--check` 的汇总行都写明了这条剔除规则与判据入口，
   下个会话不用再翻本报告即可知道「孤儿不是全剔」。
