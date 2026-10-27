# -*- coding: utf-8 -*-
"""gen_cardanim_clips.py — 把**逐 CardAnim 的 `AnimInfo` 位置参数**烘成一张运行期表。

背景
----
`Battle/CardAnimClips.cs` 原来把这 16 行（`UnitAttack(50)` 12 个有件的 + `UnitDamaged(60)` 4 个）
**硬写在一个 C# 字典里**（2026-10-23 第一版，`W63` 收口）。那是「数据抄进代码」，而真正的原因只是
`数据/游戏数据/animinfo_lookup.json` **不在 `Assets` 里**（运行期读不到）。本生成器把它搬出来。

数据从哪来（**第一权威**）
--------------------------
`d:/2/新解包资源/assets_full/bundle_*cardanims*_assets_all/MonoBehaviour/<CardAnim>.json` 的
`animInfo` —— 逐字段：
    `animAdressable.m_AssetGUID`（→ prefab 名）
    `startPosOption`(0x18) · `endPosOption`(0x1C) · `pointTowardsOption`(0x20)
    `shouldMoveVFX`(0x38) · `vfxDelayTime`(0x24) · `timeAtStartPos`(0x2C) · `timeMoving`(0x30)
字段名与偏移的判据：`d:/2/tools/il2cpp_out/dump.cs:25205-25243`（`AnimInfo` 类）。

CardAnim 名 → prefab 名（GUID → 资产名）这一跳**不在本脚本里重算** —— 直接用
`d:/4/Unity/数据/索引/anim_address_map.json` 的 `cardanim_to_asset`（由 `工具/gen_anim_address_map.py`
从 bundle 的 `m_Container` 反解出来的，**10,564** 条 `guid_to_asset` / 1,099 条 `cardanim_to_asset`；
⚠️ 2026-10-23 现读（原写 10,539 —— `T14` 的二进制 catalog 补集 +25 条；本脚本只读 `cardanim_to_asset`，那一节未变）。

输出（**两份逐字节相同**）
--------------------------
1. `d:/4/Unity/数据/游戏数据/cardanim_clips.json`   —— 全量、给工具/人看
2. `d:/4/Unity/MyGame/Assets/CardPresentation/Resources/cardanim_clips.json` —— 运行期那份

形状（`JsonUtility` 只吃**数组**，不吃字典）：
    { "schema": 1, "count": N, "rows": [ {"n": prefab 名, "a": CardAnim 名,
                                          "sp":0,"ep":0,"pt":0,"mv":0,"vd":0,"ts":0,"tm":0,"te":0,
                                          "otv":0, "ease":[t,v,…],
                                          "ao":0/1,"ont":0/1,"ofx":[…],"ofy":[…],"ofz":[…],
                                          "mnx":0,"mxx":0,"mny":0,"mxy":0,"mnz":0,"mxz":0,
                                          "mvt":0,"mvd":0,"mva":0,"mvvis":0,"mvxz":0,"mvy":0,
                                          "mvcx":[…],"mvcy":[…],"mvcz":[…],"mox":0,"moy":0,"moz":0,
                                          "shk":"Shake Hit Medium","shkd":0,"shka":0,"shkf":0,
                                          "shkoa":0,"shkof":0}, … ] }
  🆕 2026-10-23（`新账-83`）：`otv` 每行都有；`ease` 只写 `mv==1` 的行；
  `ao/ont/ofx/ofy/ofz/mn*/mx*` 只写 `ao==1` 的行（其余行缺省 = 0 / 空）—— 与
  `Resources/card_anim_order.json` 的 `CardAnimBeat` 同一套短名与同一份曲线格式。
  🆕 2026-10-25（执行代理 `TL`）：**`minionMovementInfo` 12 列 + `screenShake` 6 列**（见下一条）。
  🆕 2026-10-27（执行代理 `B9_W12USHK`）：**`unitsShake` → `ush`（数组列，259 行 / 273 条）** 与
  **`minionAnimationClipInfo` → `mac*`（2 行，`enable` 全 0 / clip 全 null）**。两族原来**一整片零数据**；
  `unitsShake` 那一路的运行期消费口（`CardFeel.ResolveUnitShake`）早就在、一直是 0 调用点。
  判据与本列语义见 `_unit_shake_block` / `_minion_clip_block` 的 doc。

🆕 2026-10-25（执行代理 `TL` · 消费端 `AU1` 已接、只差数据列）：**两族的「运行期投影」列名**
--------------------------------------------------------------------------
`Battle/CardAnimClips.cs` 的 `RtRow` 读的是**运行期短名**，而 `_minion_move_block` / `_shake_block`
原来只写 `mm*` / `ss*` ⇒ 消费端读不到。现在两边**都写**：
    `mvt mvd mva mvvis mvxz mvy mvcx mvcy mvcz mox moy moz`  ← `minionMovementInfo`（12 列）
    `shk shkd shka shkf shkoa shkof`                         ← `screenShake`（6 列；`shk` = preset 名）
  · `mm*` / `ss*` 那份**保留**：它是**全量记录** —— `mmRx*/mmRy*/mmRz*`（三轴旋转）与
    `ssOvSustain` / `ssSustain` / `ssOvDir` / `ssDx,ssDy,ssDz` / `ssOvAttack` / `ssAttack` /
    `ssOvDecay` / `ssDecay` 运行期今天**没有对应字段**（`RtRow` 只收了 amplitude/frequency 两组）。
    ⇒ 两族并存 = 「原样记录」+「运行期契约」，⛔ 不是两份真值。
  · `mva`（= `animTime`）以前**整列没写**（`_minion_move_block` 只读 delay），本次补上 ——
    没有它，消费端那条「这一拍时长 = `delay + animTime`」会算成 `delay + 0`。

🆕 2026-10-24（执行代理 `W2` · `R7` / `R13` / `R14`）：**三个原来一整片零消费的 `animInfo` 字段**
（本次只做**数据**；运行期消费由别的写手接 —— 见报告 §「运行期还需要谁接」）：
    `sd`   ← `startDelay`（**只在非 0 时写**；全池现读 1 行 = `Cuts Brutal Warp Spider` 0.5 s）
             ⚠️ `GetAnimDuration()` **不含**它 ⇒ 它影响**命中/生效那一帧**，不是时长。
    `ss*`  ← `screenShake`（**只在非默认时写**；现读 57 行）：`ssPid`（`presetSO.m_PathID` 十进制字符串）·
             `ssPreset`（pathID 反查出的 preset 名；解不出**不写**，⛔ 不猜）· `ssDelay` ·
             6 组 `ssOv*`（`overwrite*` 开关）+ 6 组值（`ssSustain` / `ssAmp` / `ssFreq` /
             `ssDx,ssDy,ssDz` / `ssAttack` / `ssDecay`）。判据见 `_shake_block` 的 doc。
    `mm*`  ← `minionMovementInfo`（**只在非默认时写**；现读 9 行）：`mmDelay` `mmAct` `mmVis`
             `mmUseXZ` `mmUseY` `mmX/Y/Z` · `mmCx/mmCy/mmCz`（位移曲线，非空才写）·
             `mmRxE/mmRxM/mmRxC`（旋转轴 enable / maxValue / 曲线），Y、Z 同形。
             判据见 `_minion_move_block` 的 doc。
    ⚠️ 三族都是**卡体/相机自己的**参数，不是 VFX 的；⛔ 别把它们并进 `sp/ep/pt/mv` 那一套
    （那是「VFX 怎么飞」）。
· 🔴 **键 = `CardAnim` 资产名（`a`），⛔ 不是 prefab 名**：同一个 prefab 名会被多张 `CardAnim`
  共用、参数却不同（2026-10-23 现读 **215 组**，例：`Trait_GainQuest` 被
  `Trait_GainQuest_self`(sp=0) 与 `Trait_GainQuest_enemywarlord`(sp=4) 共用）⇒ 按 prefab 名查必错。
  运行期那一侧由 `VfxMap.ResolvedVfx.Anim`（= `CardPick.A`）把 `CardAnim` 名交出来。
· `n`（prefab 名）仍然带上，只作**诊断/人读**用。
· **按 `a` 升序**排（确定性）。
· 🔴 **不写时间戳** ⇒ 连跑两遍 **md5 逐字节相同**（幂等）。

同名 prefab 怎么办
------------------
**不合并**：每个 `CardAnim` 各一行（键是 `a`）⇒ 215 组「同 prefab 名不同参数」在这张表里**各行其是**，
运行期按 `a` 查，一个字都不会串。脚本只会把「同一个 `n` 出现几次」打进统计（诊断用，不是冲突）。

用法
----
    PY=D:/2/Warpforge_tools/py312/python.exe
    $PY d:/4/Unity/工具/gen_cardanim_clips.py            # 生成 + 打印统计
    $PY d:/4/Unity/工具/gen_cardanim_clips.py --check    # 只比对，不写（CI 用：退出码 1 = 与盘上不一致）
"""

import argparse
import glob
import io
import json
import os
import sys

# 🆕 2026-10-24（`W2`）：生成期发现的**非致命**问题（解不出 preset 名 / preset 不在数值表里 …）
#   —— 与其它生成器同一口径：**不静默**，打印 + 进 stdout 汇总（⛔ 不写进产物，免得产物不稳）。
PROBLEMS = []

ASSETS_FULL = r"d:/2/新解包资源/assets_full"
ADDR_MAP = r"d:/4/Unity/数据/索引/anim_address_map.json"
OUT_FULL = r"d:/4/Unity/数据/游戏数据/cardanim_clips.json"
OUT_RUNTIME = r"d:/4/Unity/MyGame/Assets/CardPresentation/Resources/cardanim_clips.json"
# 🔴 2026-10-24（执行代理 `W2` · `R7`/`R13`/`R14`）：`screenShake` 的 `presetSO` 是一个
#   **没有 type tree 的 PPtr**（`{"m_FileID":2,"m_PathID":-4556…}`），`assets_full` 那批 JSON
#   不带外部引用表 ⇒ 名字只能从**旧解包**的 `<名字>_<pathID>.json` 文件名反查。
#   这与 `工具/gen_shake_presets.py` **同一个索引目录、同一个办法**（那边也用它翻 `rawSignal`）。
SHAKE_IDX_DIR = r"d:/2/解包整理/09_游戏数据/动画曲线/MonoBehaviour"
# 🔴 `CameraShakePresetSO` 的 `m_Script.m_PathID`（2026-10-25 · `TL` 现核）：
#   `assets_full/bundle_tweenandshakes_assets_all/MonoBehaviour/Shake *.json` **21 份全是这个数**；
#   旧解包索引目录里同 pid 的正好 **17 份**（= `shake_presets.json` 的 17 个 preset，逐名一致）。
#   ⇒ 用它把「屏震 preset」从那 148 份混杂的 SO 里挑出来（目录里还有 tween 等别的类）。
SHAKE_SCRIPT_PID = 3633289390197731284
# 运行期屏震数值表（`工具/gen_shake_presets.py` 的产物）—— 只用来**交叉校验**解析出的名字在不在里面
SHAKE_PRESETS_JSON = r"d:/4/Unity/MyGame/Assets/Resources/WarpforgeVFX/shake_presets.json"


def _curve_flat(curve):
    """`AnimationCurve`（全量件里的序列化形状）→ 扁平 `[t0,v0,t1,v1,…]`（按 time 升序、4 位小数）。

    🆕 2026-10-23（`新账-83`）：与 `gen_card_anim_order.py` 的 `_curve_flat` **同一个格式约定**
    （运行期 `CardAnimOrder.EaseAt` / `EaseAtRaw` 按分段线性读它）。两处是**同一个数据格式**，
    不是同一条业务规则 —— 但**改约定要两边一起改**（C# 只有一份读取实现）。
    没有键 / 曲线是 null ⇒ 空列表。
    """
    cur = ((curve or {}).get("m_Curve") or [])
    out = []
    for k in sorted(cur, key=lambda x: x.get("time", 0.0)):
        out.append(round(float(k.get("time") or 0.0), 4))
        out.append(round(float(k.get("value") or 0.0), 4))
    return out


def _preset_id_index():
    """旧解包的 `<名字>_<pathID>.json` 文件名 → `{str(pathID): 名字}`。

    🔴 **判据不是文件名，是文件内容**（2026-10-25 · `TL`）：
    · `m_Script.m_PathID` 必须 == `SHAKE_SCRIPT_PID`（`CameraShakePresetSO` 这个类的脚本 pid；
      判据 = `assets_full/bundle_tweenandshakes_assets_all/MonoBehaviour/Shake *.json` **21 份全是它**）
      ⇒ 这个目录里混着 tween / 别的 SO（现读 148 份），**不按类过滤会拿进 `AeldariRecallTween` 这种名字**；
    · `m_Name` 与文件名前缀**必须一致**，不一致时**以 `m_Name` 为准并记进 `_PRESET_IDX_RENAMED`**（出声）。
    过滤后现读 **17 份 / 17 个 pathID**，与 `shake_presets.json` 的 17 个 preset **一一对上**。

    ⚠️ 目录不在 / 一个都没有 ⇒ **回空字典并出声**（⛔ 不许静默猜名字）。
    同一个 pathID 出现多个名字 ⇒ 保留**第一个**并记进 `_PRESET_IDX_DUP`（诊断用）。
    """
    out = {}
    if not os.path.isdir(SHAKE_IDX_DIR):
        print("   ⚠️ 屏震 preset 的 pathID 索引目录不在：%s ⇒ **没查**（`ssPreset`/`shk` 不写，"
              "`ssPid` 照写；⛔ 不猜名字）" % SHAKE_IDX_DIR)
        return out
    for p in sorted(glob.glob(os.path.join(SHAKE_IDX_DIR, "*.json"))):
        b = os.path.basename(p)[:-5]
        if "_" not in b:
            continue
        nm, pid = b.rsplit("_", 1)
        if not pid.lstrip("-").isdigit():
            continue
        try:
            j = json.load(io.open(p, encoding="utf-8", errors="replace"))
        except Exception as e:                          # noqa: BLE001
            _PRESET_IDX_BAD.append((os.path.basename(p), "json 读不了：%s" % e))
            continue
        sp = ((j.get("m_Script") or {}) if isinstance(j.get("m_Script"), dict) else {}).get("m_PathID")
        if sp != SHAKE_SCRIPT_PID:
            # 不是 CameraShakePresetSO（tween / 别的 SO）—— 正常情况，静默跳过，只计数。
            _PRESET_IDX_OTHER[0] += 1
            continue
        m_name = j.get("m_Name")
        if m_name != nm:
            _PRESET_IDX_RENAMED.append((os.path.basename(p), nm, m_name))
            nm = m_name or nm                       # 内容里的名字优先
        if pid in out and out[pid] != nm:
            _PRESET_IDX_DUP.append((pid, out[pid], nm))
            continue
        out.setdefault(pid, nm)
    if not out:
        print("   ⚠️ 屏震 preset 的 pathID 索引目录里没有任何 `m_Script == %d` 的 `<名字>_<pathID>.json`："
              "%s ⇒ **没查**" % (SHAKE_SCRIPT_PID, SHAKE_IDX_DIR))
    return out


_PRESET_IDX_DUP = []
_PRESET_IDX_RENAMED = []     # (文件, 文件名里的名字, 内容 m_Name 里的名字) —— 现读 0 条
_PRESET_IDX_BAD = []         # (文件, 原因) —— 读不了的
_PRESET_IDX_OTHER = [0]      # `m_Script` 不是 CameraShakePresetSO 的份数（正常情况，只计数）


def _known_preset_names():
    """运行期屏震数值表里有的 preset 名（`gen_shake_presets.py` 的产物）。读不到 ⇒ `None`（= 没查）。"""
    try:
        d = json.load(io.open(SHAKE_PRESETS_JSON, encoding="utf-8"))
        return set(p.get("name") for p in (d.get("presets") or []) if p.get("name"))
    except Exception:                                   # noqa: BLE001
        return None


def _shake_block(ca, ai, pid2name, known):
    """`animInfo.screenShake` → 短名列（**只在非默认时**返回非空 dict；全默认 ⇒ `{}`）。

    字段与原版的对应（`d:/2/tools/il2cpp_out/dump.cs` 的 `AnimInfo.screenShake` 那一块）：
        `ssPid`    ← `presetSO.m_PathID`（**原样搬，十进制字符串**；`m_FileID` 恒 2 = 同族的卡动画束）
        `ssPreset` / `shk` ← 上面那个 pathID 反查出来的 **preset 名**（解不出 ⇒ 不写，⛔ 不猜）
        `ssDelay`  / `shkd` ← `delay`
        `ssOvSustain` / `ssSustain`         ← `overwriteSustainTime` / `sustainTime`
        `ssOvAmp`     / `ssAmp` / `shkoa` / `shka` ← `overwriteAmplitude` / `amplitude`
        `ssOvFreq`    / `ssFreq` / `shkof` / `shkf` ← `overwriteFrequency` / `frequency`
        `ssOvDir`     / `ssDx` `ssDy` `ssDz`← `overwriteDirection` / `direction.{x,y,z}`
        `ssOvAttack`  / `ssAttack`          ← `overwriteAttackTime` / `attackTime`
        `ssOvDecay`   / `ssDecay`           ← `overwriteDecayTime` / `decayTime`
    🆕 2026-10-25（执行代理 `TL`）：**`shk*` 六列 = 运行期投影**（`CardAnimClips.RtRow` 的列名，
    消费点 `BattleDriver.PlayBeatShake`）—— `shk`(名) `shkd`(delay) `shka`(amplitude)
    `shkf`(frequency) `shkoa`(overwriteAmplitude) `shkof`(overwriteFrequency)。
    `ss*` 那份**保留**（sustain/direction/attack/decay 四组运行期今天没有字段）。
    ⚠️ **`shk` 解不出就留空**（消费端 `ShakePreset` 空 = 这一拍不震）—— 但**出声**（`PROBLEMS`），
    ⛔ 不拿默认 preset 顶替（顶替 = 静默换了一个震动幅度，没人会去核）。
    ⚠️ **消费语义（原版，留给运行期写手）**：`overwrite* == 0` 的项**用 preset 自己的值**，
    `== 1` 的项用这里记的值（`OverwriteCameraShakePreset.GetModifiedPreset()` → `CameraShakerManager.DoShake`）。
    ⇒ 所以 6 个 `ssOv*` **必须和 6 个值一起搬**，只搬值不搬开关等于猜。
    """
    ss = (ai.get("screenShake") or {})
    ps = (ss.get("presetSO") or {})
    pid = ps.get("m_PathID") or 0
    ov = {k: int(ss.get(k) or 0) for k in ("overwriteSustainTime", "overwriteAmplitude",
                                           "overwriteFrequency", "overwriteDirection",
                                           "overwriteAttackTime", "overwriteDecayTime")}
    d = float(ss.get("delay") or 0.0)
    if not pid and not any(ov.values()) and not d:
        return {}
    out = {"ssPid": str(pid)}
    nm = pid2name.get(str(pid)) if pid else None
    if nm:
        out["ssPreset"] = nm
        if known is not None and nm not in known:
            PROBLEMS.append("屏震：CardAnim `%s` 引用的 preset `%s` **不在** `%s` 的 presets 里（pathID %s）"
                            "⇒ 运行期没有它的数值" % (ca, nm, os.path.basename(SHAKE_PRESETS_JSON), pid))
    elif pid:
        # 🔴 对不上 ⇒ **出声并留空**（`shk` 不写；消费端 = 这一拍不震）。⛔ 不给默认 preset。
        PROBLEMS.append("屏震：CardAnim `%s` 的 `presetSO.m_PathID = %s` **反查不到 preset 名**"
                        "（索引 = `%s` 里 `m_Script.m_PathID == %d` 的那些文件的 `m_Name`）"
                        "⇒ `shk` **留空**（这一拍不震），⛔ 没给默认 preset"
                        % (ca, pid, os.path.basename(SHAKE_IDX_DIR), SHAKE_SCRIPT_PID))
    out["ssDelay"] = round(d, 6)
    out["ssOvSustain"] = ov["overwriteSustainTime"]
    out["ssSustain"] = round(float(ss.get("sustainTime") or 0.0), 6)
    out["ssOvAmp"] = ov["overwriteAmplitude"]
    out["ssAmp"] = round(float(ss.get("amplitude") or 0.0), 6)
    out["ssOvFreq"] = ov["overwriteFrequency"]
    out["ssFreq"] = round(float(ss.get("frequency") or 0.0), 6)
    out["ssOvDir"] = ov["overwriteDirection"]
    dr = ss.get("direction") or {}
    out["ssDx"] = round(float(dr.get("x") or 0.0), 6)
    out["ssDy"] = round(float(dr.get("y") or 0.0), 6)
    out["ssDz"] = round(float(dr.get("z") or 0.0), 6)
    out["ssOvAttack"] = ov["overwriteAttackTime"]
    out["ssAttack"] = round(float(ss.get("attackTime") or 0.0), 6)
    out["ssOvDecay"] = ov["overwriteDecayTime"]
    out["ssDecay"] = round(float(ss.get("decayTime") or 0.0), 6)
    # ── 🆕 2026-10-25（`TL`）：**运行期投影**（`RtRow.shk/shkd/shka/shkf/shkoa/shkof`）──
    if nm:
        out["shk"] = nm
    out["shkd"] = out["ssDelay"]
    out["shka"] = out["ssAmp"]
    out["shkf"] = out["ssFreq"]
    out["shkoa"] = ov["overwriteAmplitude"]
    out["shkof"] = ov["overwriteFrequency"]
    return out


def _minion_move_block(ai):
    """`animInfo.minionMovementInfo` → 短名列（**只在非默认时**返回非空 dict）。

    原版结构（`d:/2/tools/il2cpp_out/dump.cs` 的 `MinionMovementInfo`，偏移逐条现核）：
        `delay`(0x10) · `shouldActivateMinionMovement`(0x14) · `visibilityStateAfterFinish`(0x15) ·
        `minionMovementAnimCurve{X,Y,Z}`(0x18/0x20/0x28) · `useXZCurve`(0x30) · `useYCurve`(0x31) ·
        `yOffset`(0x34) / `xOffset`(0x38) / `zOffset`(0x3C) ·
        `rotate{X,Y,Z}Axis`(0x40/0x48/0x50) = `{enable, curve, maxValue}` · `animTime`(0x58)
    短名（`mm` 前缀 = **全量记录**；曲线格式与 `ease` 同一套 `[t,v,…]`）：
        `mmDelay` `mmAct` `mmVis` `mmUseXZ` `mmUseY` `mmX/Y/Z`（偏移）
        `mmCx/mmCy/mmCz`（三条位移曲线，**非空才写**）
        `mmRxE/mmRxM/mmRxC`（旋转 X 的 enable / maxValue / 曲线），Y、Z 同形。
    🆕 2026-10-25（执行代理 `TL`）：**`mv*/mo*` 12 列 = 运行期投影**（`CardAnimClips.RtRow` 的列名）：
        `mvt`(shouldActivate) `mvd`(delay) `mva`(**animTime**) `mvvis`(visibilityStateAfterFinish)
        `mvxz`(useXZCurve) `mvy`(useYCurve) `mvcx/mvcy/mvcz`(三条曲线，非空才写)
        `mox/moy/moz`(xOffset/yOffset/zOffset)
      🔴 `mva`（`animTime`）以前**整列没写过** —— 消费端那条「这一拍时长 = `delay + animTime`」
      会退化成 `delay + 0`。现读全表只有 **2 条** `shouldActivateMinionMovement=1`
      （`Scarab_Explosion` / `SporeMine_Explosion`，`animTime` 都 = 0.6）；
      另有 4 条 `DA Teleport *` 只有 `yOffset=3 / animTime=1`（`act=0` ⇒ 消费端不用它的时长）。
    ⚠️ **消费语义（原版，留给运行期写手）**：这一族是**卡体自己**的位移/旋转
    （`CardScript.MoveAndDisappearAnim`，`_d__358`）；`shouldActivateMinionMovement=1`
    的两件是「爆炸时卡体位移」，**不是 VFX 的位移** —— 与 `sp/ep/mv` 那一套（VFX 的）分开。
    """
    m = (ai.get("minionMovementInfo") or {})
    if not m:
        return {}
    rot = {k: (m.get(k) or {}) for k in ("rotateXAxis", "rotateYAxis", "rotateZAxis")}
    curves = {k: _curve_flat(m.get(k)) for k in ("minionMovementAnimCurveX",
                                                 "minionMovementAnimCurveY",
                                                 "minionMovementAnimCurveZ")}
    act = int(m.get("shouldActivateMinionMovement") or 0)
    vis = int(m.get("visibilityStateAfterFinish") or 0)
    dly = float(m.get("delay") or 0.0)
    atime = float(m.get("animTime") or 0.0)
    uxz = int(m.get("useXZCurve") or 0)
    uy = int(m.get("useYCurve") or 0)
    ox = float(m.get("xOffset") or 0.0)
    oy = float(m.get("yOffset") or 0.0)
    oz = float(m.get("zOffset") or 0.0)
    nondef = (act or vis or dly or atime or ox or oy or oz or uxz or uy
              or any(curves.values())
              or any(int(r.get("enable") or 0) or _curve_flat(r.get("curve")) for r in rot.values()))
    if not nondef:
        return {}
    out = {
        "mmDelay": round(dly, 6),
        "mmAct": act,
        "mmVis": vis,
        "mmUseXZ": uxz,
        "mmUseY": uy,
        "mmX": round(ox, 6),
        "mmY": round(oy, 6),
        "mmZ": round(oz, 6),
        # ── 🆕 2026-10-25（`TL`）：**运行期投影**（`RtRow.mvt/mvd/mva/mvvis/mvxz/mvy/mox/moy/moz`）──
        "mvt": act,
        "mvd": round(dly, 6),
        "mva": round(atime, 6),
        "mvvis": vis,
        "mvxz": uxz,
        "mvy": uy,
        "mox": round(ox, 6),
        "moy": round(oy, 6),
        "moz": round(oz, 6),
    }
    for key, short in (("minionMovementAnimCurveX", "mmCx"),
                       ("minionMovementAnimCurveY", "mmCy"),
                       ("minionMovementAnimCurveZ", "mmCz")):
        if curves[key]:
            out[short] = curves[key]
    for key, short in (("minionMovementAnimCurveX", "mvcx"),
                       ("minionMovementAnimCurveY", "mvcy"),
                       ("minionMovementAnimCurveZ", "mvcz")):
        if curves[key]:
            out[short] = curves[key]
    for ax, short in (("rotateXAxis", "mmRx"), ("rotateYAxis", "mmRy"), ("rotateZAxis", "mmRz")):
        r = rot[ax]
        out[short + "E"] = int(r.get("enable") or 0)
        out[short + "M"] = round(float(r.get("maxValue") or 0.0), 6)
        c = _curve_flat(r.get("curve"))
        if c:
            out[short + "C"] = c
    return out


def _unit_shake_block(ai):
    """`animInfo.unitsShake`（`AnimInfo +0x78`，`List<UnitShake.UnitShakeInfo>`）→ `ush` 数组列。

    🆕 2026-10-27（执行代理 `B9_W12USHK`）：这一族**原来整条没有数据链**（运行期 `CardFeel.ResolveUnitShake`
    已经实现、0 调用点）。判据（🥇 逐句）：
      · 字段表 `d:/2/tools/il2cpp_out/dump.cs:25242`（`public List<UnitShake.UnitShakeInfo> unitsShake; // 0x78`）
        + `:25498-25511`（`UnitShake.UnitShakeInfo` 五个字段：`targetUnit 0x10` · `awayFromUnit 0x14` ·
        `shakeSize 0x18` · `shakeDuration 0x1C` · `shakeDelay 0x20`）；
      · 消费点 `decomp_full/BattleManager._ResolveAnimInfoRangeAttack_d__468__MoveNext.c:64-108`
        （`unitsShake` 非空 ⇒ **逐条** `ResolveUnitShake(new UnitShake(info, actingCard, targetCard))`，
        **否则**才走 `tweenAnim(+0x80)` —— 是 if/else）与 `BattleManager._ResolveAnim_d__466__MoveNext.c:108-125`
        （**同一段代码**，即 `play`/`ability`/`trigger`/`attack_melee` 那些桶也吃这张表 ⇒ 本列**与事件桶无关**）；
      · `decomp_full/UnitShake__.ctor.c:8-18`（`+0x20 = info+0x18` · `+0x24 = info+0x1c` · `+0x28 = info+0x20` ·
        `+0x10/+0x18 = GetTargetUnit(info+0x10 / info+0x14, actingCard, targetCard)`）。

    短名（**运行期 `CardAnimClips.RtUnitShake` 的列名，逐个对应，⛔ 别改名**）：
        `t`  ← `targetUnit`（`TargetUnit` 枚举**原样搬**：0 undefined · 10 self · 20 cardTarget ·
              30 abilityTarget · 40 friendlyHero · 50 enemyHero —— 判据 `dump.cs:45603-45613`）
        `aw` ← `awayFromUnit`（同上枚举）—— **只有 `aw` 非 0 且解出的卡 ≠ `t` 解出的卡时才走击退支**
        `sz` ← `shakeSize`（`UnitSize` 枚举：0 small · 2 medium · 3 big —— `dump.cs:101810-101817`；
              ⚠️ **不是 0/1/2**）
        `du` ← `shakeDuration`（**秒**；只对抖动支有意义，击退支原版把 `DoPushBack` 的 duration 写死 0.4）
        `dl` ← `shakeDelay`（**秒**；>0 时 `WaitForSeconds` 之后再判 `IsInPlay` 与二选一）

    🔴 **`t == 0`（undefined）原版是「解不出目标」**：`BattleManagerSupport__GetTargetUnit.c` 对
    0/其它值一律 `return 0` ⇒ 协程在 `:51-54` 直接 `return 0`（**什么都不做**，只白等 `dl`）。
    现读 273 条里 **237 条是 `t = 0`** ⇒ 真正有效的只有 **36 条 / 35 个 CardAnim**。
    ⛔ 别把「有这一条」当成「一定会抖」—— 运行期必须照 `GetTargetUnit` 判，⛔ 不许拿 `targetCard` 顶 `t=0`。

    ⛔ **只在 `unitsShake` 非空时写**（其余行缺省 = 空数组 = 原版这一拍没有 unit shake）。
    返回 `None` 表示不写。
    """
    us = ai.get("unitsShake") or []
    if not us:
        return None
    out = []
    for e in us:
        out.append({
            "t": int(e.get("targetUnit") or 0),
            "aw": int(e.get("awayFromUnit") or 0),
            "sz": int(e.get("shakeSize") or 0),
            "du": round(float(e.get("shakeDuration") or 0.0), 6),
            "dl": round(float(e.get("shakeDelay") or 0.0), 6),
        })
    return out


def _minion_clip_block(ai):
    """`animInfo.minionAnimationClipInfo`（`AnimInfo +0x68`）→ `mac*` 列（**只在非默认时**写）。

    🆕 2026-10-27（执行代理 `B9_W12USHK`）：**只补列定义与读数口** —— 本 build 的数据现状是
    **2 件非空且 `animationClip` 全是 null**（见下），所以**今天没有可观测差**，⛔ 不假装有。

    判据（🥇 逐句）：`d:/2/tools/il2cpp_out/dump.cs:25239`（`minionAnimationClipInfo // 0x68`）·
    `:25310-25340`（`MinionAnimClipInfo{enable 0x10, delay 0x14, minionClips 0x18[]}`）·
    `:25259-25305`（`MinionAnimClipInfoDefinition{target 0x10, animationClip 0x18, delay 0x20,
    waitForCompletion 0x24, toggleFlyOff 0x25, safeResetOnEnd 0x26}`）·
    消费点 `BattleManager._ResolveAnim_d__466__MoveNext.c:166-171`：**`enable == 0` ⇒ 根本不调**
    `ResolveAnimAnimationClip`（⇒ 本 build 那两个件的 `minionClips` 是**死数据**）。

    短名（**运行期 `CardAnimClips.RtMinionClip` / `CardAnimBodyClip` 的列名**）：
        `macEn` ← `enable`（**0 = 这条链整段不执行**）· `macD` ← 外层 `delay`（秒）
        `macs`  ← `minionClips[]`：`t`(target，`TargetUnit` 枚举) · `clip`(`animationClip.m_PathID`
                  **十进制字符串**，`"0"` = **null 引用**/解不出 —— ⛔ 不猜名字)
                  `d`(delay 秒) · `wc`(waitForCompletion) · `tf`(toggleFlyOff) · `sr`(safeResetOnEnd)

    现读（1,099 个 CardAnim 全扫）：**2 行** = `Atk_Gauss_Tesla` / `Atk_Gauss_Tesla_Annihilator`，
    两行都是 `macEn = 0`、各一条 `macs` 且 `clip = "0"`（null）、`t = 10(self)`、`tf = sr = 1`。
    ⇒ 与 `CardAnimClips.BodyClipOf` 那段「本 build 卡体 clip 一次都不会播」的结论**互相印证**。
    """
    m = ai.get("minionAnimationClipInfo") or {}
    if not m:
        return {}
    en = int(m.get("enable") or 0)
    dly = float(m.get("delay") or 0.0)
    clips = m.get("minionClips") or []
    if not en and not dly and not clips:
        return {}
    out = {"macEn": en, "macD": round(dly, 6)}
    if clips:
        arr = []
        for c in clips:
            clip = c.get("animationClip") or {}
            pid = clip.get("m_PathID") or 0
            arr.append({
                "t": int(c.get("target") or 0),
                "clip": str(pid),
                "d": round(float(c.get("delay") or 0.0), 6),
                "wc": int(c.get("waitForCompletion") or 0),
                "tf": int(c.get("toggleFlyOff") or 0),
                "sr": int(c.get("safeResetOnEnd") or 0),
            })
        out["macs"] = arr
    return out


def build():
    am = json.load(io.open(ADDR_MAP, encoding="utf-8"))
    c2a = am["cardanim_to_asset"]
    pid2name = _preset_id_index()
    known_presets = _known_preset_names()
    rows = {}
    conflicts = []
    missing = []
    for ca, info in sorted(c2a.items()):
        # 🔴 **读 `sourceMono`，不是 `dump`**：`dump` 指的是 `cardanim_to_asset` 那个 **prefab（GameObject）**，
        #    `animInfo` 在**卡动画那个 MonoBehaviour** 上（= `sourceMono`）。拿错 → 全部字段读成默认 0，
        #    而 JSON 照样生成、形状照样对 —— 静默错（本脚本第一版就是这么错的，靠「16 行逐值比对」抓出来的）。
        src = info.get("sourceMono") or ""
        n = info.get("targetName") or ""
        if not src or not n:
            missing.append((ca, "no sourceMono/targetName"))
            continue
        p = os.path.join(ASSETS_FULL, src)
        if not os.path.exists(p):
            missing.append((ca, "sourceMono missing: " + src))
            continue
        try:
            j = json.load(io.open(p, encoding="utf-8", errors="replace"))
        except Exception as e:                                  # noqa: BLE001
            missing.append((ca, "json error: %s" % e))
            continue
        ai = j.get("animInfo") or {}
        mv = 1 if ai.get("shouldMoveVFX") else 0
        row = {
            "n": n,
            "a": ca,
            "sp": int(ai.get("startPosOption") or 0),
            "ep": int(ai.get("endPosOption") or 0),
            "pt": int(ai.get("pointTowardsOption") or 0),
            "mv": mv,
            "vd": round(float(ai.get("vfxDelayTime") or 0.0), 6),
            "ts": round(float(ai.get("timeAtStartPos") or 0.0), 6),
            "tm": round(float(ai.get("timeMoving") or 0.0), 6),
            # `timeAtEndPos` —— 播放不用它，但 `GetAnimDuration() = vd+ts+tm+te` 要用它
            # （自检拿它复核 `VfxMap.SharedRangedVfxDuration` 那些「同 prefab 名多 CardAnim」该取哪一条）
            "te": round(float(ai.get("timeAtEndPos") or 0.0), 6),
        }
        # ── 🆕 2026-10-23（`新账-83` · 弹道三列）：`AnimInfo` 的「VFX movement」那几位 ──
        #   判据（`d:/2/tools/il2cpp_out/dump.cs:25219-25234` + `CardAnimController__Update.c:36-97`）：
        #     · `easeCurve`(0x40)        —— 位移进度曲线（`Evaluate((t−ts)/tm)` 再 clamp 到 [0,1]）；
        #     · `applyMovementOffsets`(0x49) + `offsetMovementAxis`(0x50)
        #                                —— lerp **之后**逐轴叠 `curve(t) × Random.Range(min,max)`；
        #     · `orientToVelocity`(0x48) —— 移动期间每帧把实例转向**速度方向**。
        #   短名与 `Resources/card_anim_order.json` 的 `CardAnimBeat` **逐字相同**（同一套运行期读取口径）。
        #   ⚠️ `ease` 只在 `mv==1` 时写（不飞的件不读它）；`ao/ont/ofx/ofy/ofz/mn*/mx*` 只在 `ao==1` 时写 ——
        #      其余行缺省（`JsonUtility` 给默认 0 / 空数组）。
        row["otv"] = 1 if ai.get("orientToVelocity") else 0
        if mv:
            row["ease"] = _curve_flat(ai.get("easeCurve"))
        ao = 1 if ai.get("applyMovementOffsets") else 0
        if ao:
            om = ai.get("offsetMovementAxis") or {}
            row["ao"] = 1
            row["ont"] = 1 if om.get("useNonModifiedAnimTime") else 0
            row["ofx"] = _curve_flat(om.get("xAxis"))
            row["ofy"] = _curve_flat(om.get("yAxis"))
            row["ofz"] = _curve_flat(om.get("zAxis"))
            for mn_key, mx_key, range_key in (("mnx", "mxx", "minMaxXOffset"),
                                              ("mny", "mxy", "minMaxYOffset"),
                                              ("mnz", "mxz", "minMaxZOffset")):
                r = om.get(range_key) or {}
                row[mn_key] = round(float(r.get("x") or 0.0), 6)
                row[mx_key] = round(float(r.get("y") or 0.0), 6)
        # ── 🔴 2026-10-24（执行代理 `W2` · `R7` / `R13` / `R14`）：三个**原来一整片零消费**的字段 ──
        #   ① `startDelay`（`R14`；全池现读 **1 个** CardAnim = `Cuts Brutal Warp Spider` 0.5 s）——
        #      ⚠️ `GetAnimDuration()` **不含**它（原版那条公式确实不含）⇒ 它影响的是**命中/生效那一帧**，
        #      不是时长。运行期消费 = 那一拍的命中帧（`EventTiming.HitFrameLag` 同族，别的写手接）。
        #   ② `screenShake`（`R7`；全池现读 **57 个** CardAnim）—— 见 `_shake_block`。
        #       🆕 2026-10-25（`TL`）：再加 6 列**运行期投影** `shk/shkd/shka/shkf/shkoa/shkof`。
        #   ③ `minionMovementInfo`（`R13`；全池现读 **10 行**）—— 见 `_minion_move_block`。
        #       🆕 2026-10-25（`TL`）：再加 12 列**运行期投影** `mvt…moz`（含以前没写的 `mva`=animTime）。
        #   三条都**只在非默认时**写键（其余行缺省 = 0 / 空 ⇒ `JsonUtility` 的默认值恰好就是「没有」）。
        sd = round(float(ai.get("startDelay") or 0.0), 6)
        if sd:
            row["sd"] = sd
        ssb = _shake_block(ca, ai, pid2name, known_presets)
        if ssb:
            row.update(ssb)
        mmb = _minion_move_block(ai)
        if mmb:
            row.update(mmb)
        # ── 🆕 2026-10-27（执行代理 `B9_W12USHK`）：两族原来**一整片零数据**的字段 ──
        #   ④ `unitsShake`（`+0x78`；现读 **259 个 CardAnim / 273 条**）—— 见 `_unit_shake_block`。
        #      运行期消费口 = `CardFeel.ResolveUnitShake`（已实现、0 调用点，等调用方接）。
        #   ⑤ `minionAnimationClipInfo`（`+0x68`；现读 **2 行**且 `enable` 全 0、clip 全 null）
        #      —— 见 `_minion_clip_block`。**只落数据，今天无观测差**（⛔ 不假装有）。
        usb = _unit_shake_block(ai)
        if usb:
            row["ush"] = usb
        macb = _minion_clip_block(ai)
        if macb:
            row.update(macb)
        # 🔴 键 = **CardAnim 名**（`a`）。同一 prefab 名可以有多张 CardAnim、参数不同 ⇒ 各存各的。
        if ca in rows:                       # 理论上 c2a 的键唯一（现读 1,099/1,099）
            conflicts.append((ca, rows[ca]["n"], n))
            continue
        rows[ca] = row
    out = {
        "schema": 1,
        "generatedBy": "d:/4/Unity/工具/gen_cardanim_clips.py",
        "what": "逐 CardAnim 的 AnimInfo 位置/移动参数（**键 = `a`（CardAnim 名）**；`n` = prefab 名，诊断用）"
                "；判据 → 本脚本文件头。`mm*`/`ss*` = 原样全量记录；"
                "`mv*/mo*`(12) 与 `shk*`(6) = 运行期投影（= `CardAnimClips.RtRow` 的列名）"
                "（2026-10-25 · `TL`）",
        "source": "assets_full/bundle_*cardanims*_assets_all/MonoBehaviour/<CardAnim>.json 的 animInfo"
                  " + 数据/索引/anim_address_map.json 的 cardanim_to_asset",
        "count": len(rows),
        "rows": [rows[k] for k in sorted(rows)],
    }
    return out, conflicts, missing


def dump_bytes(obj):
    # 统一 utf-8 + LF + 末尾换行；⛔ 不写 generatedAt（幂等要求）
    return (json.dumps(obj, ensure_ascii=False, indent=1, sort_keys=False) + "\n").encode("utf-8")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只比对盘上那份，不写")
    args = ap.parse_args()

    out, conflicts, missing = build()
    b = dump_bytes(out)

    print("rows                     : %d" % out["count"])
    print("冲突（同 prefab 名不同参数）: %d" % len(conflicts))
    for c in conflicts[:20]:
        print("   CONFLICT %s  keep=%s  drop=%s" % c)
    print("跳过的 CardAnim            : %d" % len(missing))
    for m in missing[:10]:
        print("   SKIP %s  (%s)" % m)
    # 🆕 2026-10-24（`W2`）：三个新字段族的落表条数（`R14` / `R7` / `R13`）
    _sd = sum(1 for r in out["rows"] if "sd" in r)
    _ss = sum(1 for r in out["rows"] if "ssPid" in r)
    _ssn = sum(1 for r in out["rows"] if "ssPreset" in r)
    _mm = sum(1 for r in out["rows"] if "mmAct" in r)
    _mvt = sum(1 for r in out["rows"] if "mvt" in r)
    _mvt_on = sum(1 for r in out["rows"] if r.get("mvt"))
    _mva = sum(1 for r in out["rows"] if (r.get("mva") or 0) > 0)
    _shk = sum(1 for r in out["rows"] if "shk" in r)
    print("🆕 R14 startDelay (`sd`)   : %d 行" % _sd)
    print("🆕 R7  screenShake (`ss*`)  : %d 行（preset 名解出 %d 行 · 运行期 `shk` %d 行）"
          % (_ss, _ssn, _shk))
    print("🆕 R13 minionMovement (`mm*`): %d 行（运行期 `mvt` %d 行，其中 `mvt==1` **%d 行**；`mva>0` %d 行）"
          % (_mm, _mvt, _mvt_on, _mva))
    # 🆕 2026-10-27（`B9_W12USHK`）：`unitsShake` / `minionAnimationClipInfo` 两族
    _ush = [r for r in out["rows"] if r.get("ush")]
    _ush_n = sum(len(r["ush"]) for r in _ush)
    _ush_eff = sum(1 for r in _ush for e in r["ush"] if e["t"])
    _mac = [r for r in out["rows"] if "macEn" in r]
    _mac_on = sum(1 for r in _mac if r["macEn"])
    _mac_clip = sum(1 for r in _mac for c in (r.get("macs") or []) if c["clip"] != "0")
    print("🆕 unitsShake (`ush`)      : %d 行 / %d 条（`t != 0` 真正有效的 **%d 条**；"
          "`t==0` 原版解不出目标 ⇒ 空转）" % (len(_ush), _ush_n, _ush_eff))
    print("🆕 minionAnimationClip (`mac*`): %d 行（`macEn==1` **%d 行**；`clip` 非 null 的 **%d 条**）"
          % (len(_mac), _mac_on, _mac_clip))
    if _PRESET_IDX_RENAMED:
        print("   ⚠️ 索引里 `m_Name` 与文件名不一致（已按 `m_Name`）：%s" % _PRESET_IDX_RENAMED[:5])
    if _PRESET_IDX_BAD:
        print("   ⚠️ 索引里有读不了的 json：%s" % _PRESET_IDX_BAD[:5])
    print("   索引目录：`m_Script` 不是 CameraShakePresetSO 的 %d 份（正常，已跳过）"
          % _PRESET_IDX_OTHER[0])
    if _PRESET_IDX_DUP:
        print("   ⚠️ 同一个 pathID 反查出多个名字（保留第一个）：%s" % _PRESET_IDX_DUP[:5])
    if PROBLEMS:
        print("   ⚠️ 问题 %d 条：" % len(PROBLEMS))
        for p in PROBLEMS[:10]:
            print("      -", p)

    if args.check:
        bad = 0
        for p in (OUT_FULL, OUT_RUNTIME):
            if not os.path.exists(p):
                print("MISSING " + p)
                bad += 1
                continue
            cur = io.open(p, "rb").read()
            if cur != b:
                print("DIFF " + p)
                bad += 1
        print("check: %s" % ("OK" if bad == 0 else "%d 处不一致" % bad))
        return 1 if bad else 0

    for p in (OUT_FULL, OUT_RUNTIME):
        os.makedirs(os.path.dirname(p), exist_ok=True)
        io.open(p, "wb").write(b)
        print("wrote %s  (%d B)" % (p, len(b)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
