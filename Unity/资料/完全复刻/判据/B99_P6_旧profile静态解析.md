# B99_P6 · 旧 profile 静态解析（P06 / A1809「旧机器 id / 静态盐」结案判据）

> 会话：第九十九（B99_P6）· 2026-10-30
> 目标文件（**只读**）：`d:/2/关服前存档/Warpforge/PlayerDecks39.dat`（1,211 B）
> 🔴 全程**纯静态解析字节流**：未调用 `BinaryFormatter.Deserialize` 或任何反序列化 API，未实例化任何类型，未联网，未在 `d:/2` 写一个字节。

---

## 〇 一句话结论

**真不可解（对「旧 profile 的 `deviceId` 明文」这个字面目标）** —— 但**对复刻不构成实际阻塞**：
密文背后只是那台已消失机器（Windows 用户 `shen0`）的**硬件标识 DUID**，而档里的账号字段
（`loginName` / `loginPassword` / `playFabId`）**全是空串**。盐 = 明文 = 该机器 DUID（自指），常量盐已实测排除，
本机找不到 `shen0` 机器的任何痕迹 ⇒ 该 DUID 不可回收。R4b 留下的三条「活路」**本轮已全部关闭**。

---

## 一 静态解析结果（40 字段逐格 + 自校验证据）

### 1·1 自校验（解析正确性的硬判据）

| 判据 | 实测 | 结论 |
|---|---|---|
| 字节消耗 | **1211 / 1211**（解析器读到 `MessageEnd` 恰好用尽文件） | ✅ 对齐 |
| `lastBuildSaved` | **`"1.35.0"`**（已知值，可反证） | ✅ 一致 |
| 记录结构 | `SerializedStreamHeader` → `BinaryLibrary("Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", lib=2)` → `ClassWithMembersAndTypes{oid=1, name=LocalPlayerData, mc=40, lib=2}` → `MessageEnd` | ✅ 标准 BF |

- 关键偏移：Header `@0x0000`(17B) · BinaryLibrary `@0x0011` · 根类 `@0x005d` · `MessageEnd` `@0x04ba`。
- 安全说明：解析器自行按 MS-NRBF 记录格式逐字节读（`0x00` header、`0x0C` BinaryLibrary、`0x05` ClassWithMembersAndTypes、成员名表 + `BinaryTypeEnum` 表 + `AdditionalInfos` + `LibraryId` + 成员值；string 成员是**记录式** `0x06 BinaryObjectString{oid,str}` / `0x09 MemberReference{idref}` / `0x0A ObjectNull`），**不实例化任何类型**。

### 1·2 40 字段逐格（`LocalPlayerData`，字段名/顺序照 `out_cpp2il_dev_ilspy/LocalPlayerData.cs` 与编译期声明序）

| # | 字段 | 值 | BF 编码 |
|--:|---|---|---|
| 1 | loginName | `''`（空串） | `String oid=3 len=0` |
| 2 | loginPassword | `''`（空串） | `MemberReference idref=3`（**指向 #1 那个空串**） |
| 3 | **deviceId** | **`dAkd61g/es6uhbbLFKmcC6VchJPlidujbxnShdBU9onSM0shYJRy3oAnodGQKspd`** | `String oid=4 len=64`（**48 字节 AES-CBC 密文**） |
| 4 | playFabId | `''`（空串） | `MemberReference idref=3`（**指向 #1 那个空串**） |
| 5 | languageVersion | 1 | Primitive int32 |
| 6 | lastRegistrationShowTime | 0 | Primitive int64 |
| 7 | disableParticles | False | Primitive bool |
| 8 | notificationsOn | True | Primitive bool |
| 9 | crateNotificationsOn | False | Primitive bool |
| 10 | chatInvisibleModeOn | False | Primitive bool |
| 11 | friendlyBattle | False | Primitive bool |
| 12 | loginWithGoogle | False | Primitive bool |
| 13 | dontUseAltPortSetting | False | Primitive bool |
| 14 | noBotsModeOn | False | Primitive bool |
| 15 | noTipsModeOn | False | Primitive bool |
| 16 | noFfullScreen | False | Primitive bool |
| 17 | musicVolume | 98 | Primitive int32 |
| 18 | voiceOversVolume | 100 | Primitive int32 |
| 19 | fxVolume | 64 | Primitive int32 |
| 20 | **dataVersion** | **39** | Primitive int32（内存偏移 = **+0x58**，见 `dump.cs:78069`） |
| 21 | useMobileBattleScreenSize | False | Primitive bool |
| 22 | lastBuildSaved | `"1.35.0"` | `String oid=6 len=6` |
| 23 | hiFPS | True | Primitive bool |
| 24 | vsync | True | Primitive bool |
| 25 | fpsLimit | 2 | Primitive int32 |
| 26 | smallScreenUI | True | Primitive bool |
| 27 | graphicsQuality | `EverguildQualitySetting.value__ = 4` | Class 记录（嵌套） |
| 28 | useSuperSampling | False | Primitive bool |
| 29 | useCombatAutoZoom | True | Primitive bool |
| 30 | graphicsExtendedCompatibility | False | Primitive bool |
| 31 | cardsTextInHandFormat | `CardTextsInHandOptions.value__ = 1` | Class 记录（嵌套） |
| 32 | cardsTextInHandFormatChosenManually | False | Primitive bool |
| 33 | graphicsQualityChosenManually | False | Primitive bool |
| 34 | touchInputChosenManually | False | Primitive bool |
| 35 | smallUIChosenManually | True | Primitive bool |
| 36 | autoCombatChosenManually | True | Primitive bool |
| 37 | touchInput | False | Primitive bool |
| 38 | unrankedMultiplayer | False | Primitive bool |
| 39 | unrankedFastMode | True | Primitive bool |
| 40 | lastTimeAskedForNotifications | 1755093982664 | Primitive int64 |

> 对照：本机 qjh36 的原版存档（`C:/Users/qjh36/AppData/LocalLow/Everguild/Warpforge/PlayerDecks39.dat`，1,203 B）同 schema，也是 `dataVersion=39`、`deviceId` 为 64 字符 base64 密文（`rNzPoC+…`）、`loginName/loginPassword=ObjectNull`、`playFabId=''`。两份档结构一致、设备标识各异。

---

## 二 算法链（回源核实，逐处出证据）

判据源：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/CryptoString.cs` · `d:/2/tools/decomp_full/PlayerDataManager__{Save,Load}PlayerData.c` · `GameStaticData__get_DeviceId.c` / `__set_DeviceId.c` · `dump.cs`。

```
SavePlayerData(PlayerDataManager__SavePlayerData.c:42-49)
  +0x18 (loginPassword) = EncryptRijndael( manager[…+0x100] )     // 空串 ⇒ EncryptRijndael 直接 return ""（不加密）
  +0x20 (deviceId)      = EncryptRijndael( GameStaticData.DeviceId )
  EncryptRijndael 内部：pwd = GetPWD()   ← 用默认 dataVersion=1（恒走 DUID 分支）

LoadPlayerData(PlayerDataManager__LoadPlayerData.c:34-35, :230-240)
  loginPassword = DecryptRijndael(+0x18, savedDataVersion=*(+0x58))
  deviceId      = DecryptRijndael(+0x20, savedDataVersion=*(+0x58))
  DeviceId(static) = <上面解出的 deviceId>   ← set_DeviceId 唯一调用点（:240）

GetPWD(dataVersion):   (CryptoString.cs:66-113)
  if (dataVersion != 0):
     d = SystemInfo.deviceUniqueIdentifier
     if (d 非空 ∧ d != "n/a" ∧ d.Length >= 5)  return d
  return "toQmbArBDRPNkMbm"          ← 常量档

GameStaticData.get_DeviceId():  (GameStaticData__get_DeviceId.c)
  if (localDeviceId(+0x148) 空/null) return SystemInfo.deviceUniqueIdentifier
  return localDeviceId
```

**⇒ 三个直接推论：**
1. **本档 `dataVersion = 39 ≠ 0`** ⇒ 解密时 `GetPWD(39)` **恒走 DUID 分支**（不是常量档）。常量盐只在 `dataVersion==0` 时才会用（而生产 `EncryptRijndael` 永远用默认 1 ⇒ 常量档在生产写入时永不发生）。
2. **`deviceId 明文 = SystemInfo.deviceUniqueIdentifier`（DUID）**，而 `deviceId` 的**盐也是 DUID** ⇒ **盐 == 明文（自指）**。也就是说：要知道明文就得知道盐，而盐就是明文本身。
3. **`loginPassword` 明文是空串**（BF 里是 `MemberReference` 指向空串，不是密文）⇒ 档里**没有第二个 DUID 派生密文**。**唯一的密文 = `deviceId`。**

---

## 三 三档判定与依据

| E0 结果 | 实测 | 命中档位？ |
|---|---|---|
| `loginPassword` 与 `deviceId` **都空** | loginPassword=空✅，但 **deviceId=非空密文**❌ | 否（`无阻塞` 不成立） |
| `dataVersion == 0` | **39** ❌ | 否（常量盐档不适用） |
| 有密文且 `dataVersion != 0` | ✅（deviceId 密文 + 39） | **是 ⇒ 进 E1/E2** |

**E1（常量盐试解）——实测排除：**
`pbkdf2_hmac("sha1", b"560A18CD-6346-4CF0-A2E8-671F9B6B9EA9", b"toQmbArBDRPNkMbm", 1000, 48)` → AES-256-CBC 解 `deviceId`：
```
raw hex : c7460652f21d95f52dfd0b30e2074d6e0252ae13d7449c5bfa3c2f091dfcf9c84baadd4a3f95819ed849f45904f34a42
pad     : 0x42(=66) —— 非法 PKCS7 填充
raw pt  : \xc7F\x06R\xf2\x1d\x95\xf5-\xfd\x0b0\xe2\x07Mn\x02R…  ⇒ 非可打印
```
⇒ 常量盐**判死**（判定式要求「全可打印 ASCII」，这里连填充都不合法）。

**E2（要旧机器 DUID）——本轮尽职排查结论：拿不到。**
- `deviceId` 明文 = `shen0` 那台机器的 DUID（自指盐）。
- 本机 MachineGuid = `d04821e2-4b3d-4ceb-aa28-e111b779d941`，用它及其 24 种变形（原样/大写/去横杠/MD5/SHA1/`{…}`/GUID 字节序）当盐，**对本档与本机 qjh36 两份密文全部不命中**（`any hit: False`）。
- `C:/Windows.old/Windows/System32/config`（旧注册表 hive）**不在**（只剩 `$Recycle.Bin` 与 `Users`）。
- `C:/Windows.old/Users/` 只有 `qjh`（≠ `shen0`），且其 Warpforge 目录里没有可用的旧 hive。
- `d:/2` 全盘按 `*.hiv / NTUSER* / SAM / SECURITY / *.reg / *shen*` 搜 ⇒ 无任何 system/注册表备份；`d:/2/关服前存档` 内 grep `shen0`（排除 .log）**0 命中**。
- 归档 `Player.log`（18.6 MB）：`deviceUniqueIdentifier/DeviceId/MachineGuid` **0 命中**；32-hex 只有全零串；36 GUID 0 个（与 R4b 一致）。
- 本机我们自己的运行产物（`C:/Users/qjh36/AppData/LocalLow/QJHWarpforge/WarpforgeReplica/*`）**未持久化 DUID**（`profile.json` 无 device 字段；日志无 DUID）。

**⇒ 三档落定：`真不可解`。** R4b 的三条活路本轮全关：① 两格并非都空（deviceId 有密文）· ② dataVersion=39≠0 · ③ 旧 MachineGuid 变形实测不命中且无任何旧 hive 可回收。

---

## 四 若「可解」的完整可复现步骤（**只写不执行**，注明当前不成立）

> 只有在拿到 `shen0` 机器的 `SystemInfo.deviceUniqueIdentifier` 时才能跑通；本机没有它 ⇒ 下列步骤**当前不可执行**，仅作留档。

```python
import base64, hashlib
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
INPUTKEY = b"560A18CD-6346-4CF0-A2E8-671F9B6B9EA9"
CT = base64.b64decode("dAkd61g/es6uhbbLFKmcC6VchJPlidujbxnShdBU9onSM0shYJRy3oAnodGQKspd")
def decrypt_with_duid(duid_str: str) -> bytes:
    k = hashlib.pbkdf2_hmac("sha1", INPUTKEY, duid_str.encode("ascii"), 1000, dklen=48)  # ASCII(GetPWD())
    key, iv = k[:32], k[32:48]
    d = Cipher(algorithms.AES(key), modes.CBC(iv)).decryptor()
    pt = d.update(CT) + d.finalize()
    pad = pt[-1]
    assert 1 <= pad <= 16 and pt.endswith(bytes([pad]) * pad), "PKCS7 不合法 ⇒ 盐错"
    return pt[:-pad]
# 判定式：解出必须全可打印 ASCII，且 == duid_str（因为盐==明文，自一致性）
```

- 触发条件：`duid_str` = `shen0` 机器当时的 `SystemInfo.deviceUniqueIdentifier`。
- 关键判据：解出的明文**必须等于所用的盐**（自指）；错盐命中率 < 1e-7。

---

## 五 已排除的路（逐条 + 实测证据）

| 路 | 排除依据 | 状态 |
|---|---|---|
| 空串不加密 ⇒ 无密文 | `deviceId` 实为 64 字符 base64 密文（48B），`dataVersion=39` | ❌ 排除 |
| 常量盐 `"toQmbArBDRPNkMbm"`（dataVersion==0 分支/DUID 非法分支） | AES 解出非法填充（pad=0x42）+ 非可打印 | ❌ 实测排除 |
| 盐 = 本机 MachineGuid 的各种变形 | 24 种变形对两份密文全不命中 | ❌ 实测排除 |
| 旧 MachineGuid 可回收（Windows.old 注册表 hive） | `Windows.old/Windows/System32/config` 不存在；`Windows.old/Users` 只有 `qjh` | ❌ 排除 |
| `d:/2` 归档里藏旧机器/注册表备份 | 全盘搜 `*.hiv/NTUSER/SAM/SECURITY/*.reg/*shen*` 无；`关服前存档` grep `shen0` 0 命中 | ❌ 排除 |
| `Player.log` 里漏出 DUID | 关键字 0 命中；32-hex 仅全零；无 36-GUID | ❌ 排除 |
| 跑原版游戏「当场解」 | `GetPWD(39)` 用的是**运行机**的 DUID，不是 `shen0` 机器的 ⇒ 换机必错 | ❌ 结构性不可行 |
| 本机自己产物里取当前 DUID 作桥 | 未持久化 DUID | ❌ 且即便取到也是本机 DUID ≠ shen0 |
| 从密文反推明文（盐==明文） | 128-bit 未知 DUID，PBKDF2-1000 无已知明文 ⇒ 暴力不可行 | ❌ 不可行 |

---

## 六 对账的影响（B98 关于 P06 / P07 / P09 的旧结论该怎么改）

### P06（`A1809` 旧机器 id / 静态盐）
- **B98_L5:93 原口径**：「**真不可解**，需旧机器 DUID，**静态盐只是一半**」。
- **改成**：方向（真不可解）**保留**，但**理由要换**——**静态盐不是「一半」，是整条被实测排除**（常量盐给出乱码）；**DUID 是唯一的盐且本地不存在**；而且**密文背后只是机器 DUID（硬件哈希），账号字段全空** ⇒ 这个「不可解」是**名义上的**，**不阻塞复刻**（我们的复刻自生成自己的 `deviceId`，见 `Shell/RandomSeedProvider.cs:206` 用的 `SystemInfo.deviceUniqueIdentifier`）。
- B98_R1x:36 的「2×2 组合 / 先试 DUID 空/长度<5」**作废**（R4b 已改成 1×2，本轮实测常量支排除 ⇒ 实际只剩 DUID 一支且不可得）。

### P07（`A186`+`A376` PlayFab TitleData）
- P06 不改变 P07 结论（PlayFab 已关服 ⇒ 后端数值不可得）。**保留「永久不可解（后端）」**。
- 但本轮在同目录发现两条**新的远端-配置缓存**值得转给**远端线/活动线**挖：
  - `d:/2/关服前存档/Warpforge/RemoteConfigCache.json`（Firebase/RemoteConfig assignment：`EnableTroopVOs`/`EnableWarlordVOs` + `configAssignmentHash`/`assignmentId`/`environmentId`，`Date: Thu, 30 Apr 2026`）。
  - `d:/2/关服前存档/Warpforge/eventcache`（`UGSEventCache` 6,058 B 二进制，一串事件 GUID）。
  ⚠️ 这两份是**RemoteConfig / 事件缓存**，**不是** PlayFab `TitleData` ⇒ **不能直接结 P07**，但可作为活动线/远端线的旁证源。

### P09（`A1232` MissionsTab 来源）
- P06 与之无关。⚠️ **B98 内部自相矛盾**：`B98_L5:96` 写「永久不可解」，而 `B98_R1x:17` 已证 `MissionsTab__CreateMissions.c` 全文 **0 次 `LoadComponent`**、三个 holder 全是**本地 PPtr** ⇒ **可本地结案**。
  ⇒ 建议按 `B98_R1x:17`（**本地可结案**）为准，删掉 `B98_L5:96` 的「永久不可解」口径（属 B98 遗留未订正项，非本轮 P06 改动）。

---

## 七 复跑（只读，命令可复跑）

```bash
# 静态解析（脚本自建在临时目录，不碰数据目录）
python -I -X utf8 <parser>.py "d:/2/关服前存档/Warpforge/PlayerDecks39.dat"
# 自校验看末行：consumed 1211 / total 1211，且 lastBuildSaved == "1.35.0"
```
