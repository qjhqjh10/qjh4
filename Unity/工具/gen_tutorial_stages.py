# -*- coding: utf-8 -*-
"""教程 6 关的【关卡脚本】→ 运行时纯数据产物（B9 · 2026-10-17）。

跑法（🔴 **必须用带 UnityPy 的解释器** —— 牌组名与音效名要开原版 bundle 查；用别的 python 会直接报错）：
    "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_tutorial_stages.py

产出（覆盖写、可重跑、幂等）：
    d:/4/Unity/MyGame/Assets/RuleEngine/Resources/tutorial_stages.json

输入（**全部只读**，铁律 2）：
    d:/2/新解包资源/assets_full/bundle_tutorialso_assets_all/MonoBehaviour/Warpforge_TutorialStage{1..6}.json
        —— 🔴 **数据源**：原版 6 个 `TutorialStage` SO
    D:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/prebuiltdecks_assets_all.bundle
        —— 按 pid 解 `playerDeck`/`aiDeck` 两个 PPtr 的**资产名**（= 教程牌组的 SO 名）
    …/tutorialwarlordchats_assets_all.bundle + 6 个 `*cardassets_assets_all.bundle`
        —— 按 pid 解 `sound.clip` 的**音效名**
    数据/游戏数据/tutorial_stages.json —— Godot 原型的**派生快照**，⚠️ **只当交叉校验**（不当数据源）
    MyGame/Assets/RuleEngine/Resources/tutorial_decks.json —— B6 那批的产物，用来交叉校验牌组名
    MyGame/Assets/RuleEngine/Resources/cards_engine.json —— 卡名 → 我们的卡 id

🔴 **判据（一条不猜，出处都写在下面）**：
    ① `ScriptedActionType` / `ScriptedActionUnit` 两张枚举表 = 逐条抄自
       `d:/2/Warpforge_code/Scripts/Assembly-CSharp/{ScriptedActionType,ScriptedActionUnit}.cs`
    ② `ScriptedAction` / `TurnScriptedData` / `TutorialStage` 的字段名与含义 = 同目录的同名 `.cs`
    ③ 🔴 **`actionType` 那个字符串是【显示名】，不是输入**：它由 `ScriptedAction.UpdateName()`
       生成（`d:/2/tools/decomp_full/ScriptedAction__UpdateName.c`），而**运行时读的是
       `scriptedActionData[0].actionType`（枚举 int）** —— 字符串只进一句 `CustomDebug.Log`
       （`AiScripted__ExecuteAction.c:59-67`）。⇒ 括号里那半**不需要"解析"**，它是**派生物**；
       本脚本**反过来**用它把 pid → 卡名解出来（规则见 `parse_action_arg()`）。
    ④ `actionType` 的字符串**原样**保留在产物里（`text` 字段）—— 复核时肉眼就能对回 SO。
    ⑤ 教程那 12 副教程牌的**玩家/敌方归属**不是本脚本的事（B6 已落 `tutorial_decks.json`）；
       本脚本只用 SO 里那两个 PPtr 去**核**它（核不上就炸）。

🔴 **与 `Unity/数据/游戏数据/tutorial_stages.json` 的关系**（文件名恰好相同，⛔ 别混）：
    那份是 Godot 原型的**派生快照**：SO 的 430 条动作只留了 152 条（`playerAction==true` 的那些）、
    `tips[]` 被单独抽出（⇒ **tip 与动作的时序没了**）、`smallTipParams`/`waitBefore`/`waitAfter`/
    `sound`/`scriptedActionData` 全没有（出处：`资料/普查产出_1017/W_B6_教程牌数据.md` §②）。
    本产物是**全量**的（430 条），且**逐条与那份交叉校验过**（见产物 `crossCheck*`）。

🔴 **两张卡走了裁定映射**（`OCR_NAME_FIX` · 2026-10-17 调度台裁定）：`Gauss Warrior (tutorial)`→`SAU10`、
    `Tesla Immortal`→`SAU12`。**判据 = `card_stats.json` 的 `ocrName` 列与 SO 名逐字吻合 + 池内唯一，
    ⛔ 不是原版明确指认**（原版卡 SO 包本地没有 ⇒ 钉不死）；产物里 **SO 原名一字未改**，`matchTier` 写成 `ocrName` 以便区分。
"""
import glob
import hashlib
import json
import os
import re
import sys
from collections import Counter, defaultdict

# ============================================================ 路径
D_SO = "d:/2/新解包资源/assets_full/bundle_tutorialso_assets_all/MonoBehaviour"
D_AAB = "D:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"
BUNDLE_DECKS = os.path.join(D_AAB, "prebuiltdecks_assets_all.bundle")
# 音效 clip 落在这些包里（按 SO 的 externals 下标算出来的，见报告「跑过的命令」）
BUNDLES_AUDIO = [
    "spacemarinesultramarinescardassets_assets_all.bundle",
    "tyranidsleviathancardassets_assets_all.bundle",
    "tutorialwarlordchats_assets_all.bundle",
    "aeldarisaimhanncardassets_assets_all.bundle",
    "necronssautekhcardassets_assets_all.bundle",
    "chaosspacemarinesblacklegioncardassets_assets_all.bundle",
    "orksgoffcardassets_assets_all.bundle",
]
SNAPSHOT = "d:/4/Unity/数据/游戏数据/tutorial_stages.json"          # 派生快照（只当交叉校验）
STATS = "d:/4/Unity/数据/游戏数据/card_stats.json"                   # 解不出时的**候选线索**来源（ocrName 列）
TUT_DECKS = "d:/4/Unity/MyGame/Assets/RuleEngine/Resources/tutorial_decks.json"
POOL = "d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
OUT = "d:/4/Unity/MyGame/Assets/RuleEngine/Resources/tutorial_stages.json"

# 产物格式版本：字段有增减就 +1（C# 侧读它）
FORMAT = 1

# ============================================================ 两张枚举表（逐条抄自 Assembly-CSharp 的 .cs 桩）
# `ScriptedActionType.cs`
ACTION_TYPE = {
    0: "None", 10: "DrawCard", 20: "PlayCard", 30: "Attack", 31: "AttackFreeMode",
    35: "ChangeToRanged", 36: "ChangeToMelee", 40: "ActiveAbility", 50: "PlayerChat",
    55: "PlayerChatBig", 60: "AiChat", 65: "AiChatBig", 70: "PlayerChoice", 80: "SmallTip",
    90: "RadioMessage", 100: "EndTurn", 110: "ClickCard", 120: "TapCard", 130: "ShowManaAura",
    140: "ResolveCard", 150: "ContinueSmallTip", 160: "LandWarlords", 170: "ActivateHandCards",
}
# `ScriptedActionUnit.cs`
ACTION_UNIT = {
    0: "None", 10: "PlayerWarlord", 20: "AiWarlord", 30: "PlayerMinion",
    31: "PlayerMinionLeft", 32: "PlayerMinionNotLeft", 40: "EnemyMinion",
    50: "PlayerCardInHand", 60: "EnemyCardInHand",
}
# `TutorialTipParams` 里那两个枚举（提示框往哪儿指）—— 同样逐条抄自
# `d:/2/Warpforge_code/Scripts/Assembly-CSharp/{PositionReference,PositionRelation}.cs`
POSITION_REFERENCE = {
    0: "Center", 10: "EnemyWarlord", 11: "EnemyWarlordMelee", 12: "EnemyWarlordRange",
    13: "EnemyWarlordHealth", 20: "PlayerWarlord", 21: "PlayerWarlordMelee",
    22: "PlayerWarlordRange", 23: "PlayerWarlordHealth", 30: "EnemyMinion", 40: "PlayerMinion",
    50: "EnemyCardInHand", 60: "PlayerCardInHand", 70: "EnemyMana", 80: "PlayerMana",
    90: "EndTurn", 100: "ActiveAbility", 110: "ChatButton", 120: "Cemetery",
}
POSITION_RELATION = {0: "Exact", 10: "LeftOf", 20: "RightOf", 30: "Above"}

# `UpdateName()` 里会把「单位显示名」追加进括号的那批 sub actionType（LAB_180641ff0 的跳转表，逐个抄下来）
ARG_IS_UNIT = {20, 30, 31, 35, 36, 40, 110, 120, 150}
# `UpdateName()` 里会把「本地化文案」追加进括号的那批（LAB_180641d32）
ARG_IS_TEXT = {50, 55, 60, 65, 80, 90}
# `UpdateName()` 里单独一条：ResolveCard(140) 也追加单位卡名（LAB_180642120）
ARG_IS_UNIT_140 = 140
# 督军那两个**字面量**（`UpdateName` 里的 DAT_1842c49a8 / DAT_1842c45a8）。
# 🔴 这两个字符串是**实测反推**的：SO 里 type=10 的动作括号里全是 "Player warlord"、
#    type=20 的全是 "Enemy warlord"（下面 `check_arg_rule` 会逐条断言，错了就炸）——
#    没有去 GameAssembly.dll 里读那两个字面量（`_DAT_` 读法见 `资料/战斗规则与数值_出处.md` §三，本批没做）。
WARLORD_ARG = {10: "Player warlord", 20: "Enemy warlord"}

# 关卡名（SO 的 `m_Name`）—— 6 关，缺一个多一个都炸
STAGE_NAMES = ["Warpforge_TutorialStage%d" % i for i in range(1, 7)]

# 交叉校验派生快照时的 strip 规则（**实测反推**：那份快照保留了 `<b>`，只去掉了 link/nobr/sprite，`<br>`→换行）
SNAP_STRIP = re.compile(r"<link=[^>]*>|</link>|<nobr>|</nobr>|<sprite name=[^>]*>")


def load(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def norm(s):
    """归一化：与 `工具/gen_prebuilt_decks.py` / `check_prebuilt_decks.py` **逐字一致**
    （小写 / 去撇号 / 去括号内容 / 去尾部独立数字 / 去非字母数字 / >4 字去尾 s）。"""
    s = s.lower().replace("’", "").replace("'", "").replace("‘", "").replace("`", "").replace("´", "")
    s = re.sub(r"\([^)]*\)", " ", s)
    s = re.sub(r"\s+\d+$", "", s.strip())
    s = re.sub(r"[^a-z0-9]+", "", s)
    return s.rstrip("s") if len(s) > 4 else s


# ============================================================ ① 读 SO + 把 actionType 字符串拆开
def parse_action_arg(at):
    """把 SO 的 `actionType` 字符串拆成 `(枚举名, 括号里那半 or "")`。

    🔴 **为什么能这么拆**（= `ScriptedAction.UpdateName()` 的逆，`decomp_full/ScriptedAction__UpdateName.c`）：
        起手 = `System.Enum.ToString(scriptedActionData[0].actionType)` ⇒ **枚举名**；
        再按 sub actionType 决定要不要 `Concat(" (", X, ")")`：
          · `ARG_IS_UNIT` 那批 ⇒ X = actingUnitType==10 → "Player warlord" / ==20 → "Enemy warlord" /
            否则 = `actingUnit`(RawCardScript) 的**卡名**（`+0x60`）；ref 为空则**什么都不加**
          · `ARG_IS_TEXT` 那批 ⇒ X = `textReference` 经本地化表查出来的**文案**（表在远端 ⇒ 我们只能拿 SO 里的英文）
          · `ResolveCard 140` ⇒ 同 ARG_IS_UNIT 的第三档（有 ref 才加）
    ⚠️ 所以括号里那半**不是输入**：动作类 = 显示名（只作对照），聊天/提示类 = 要显示的文案。
    """
    name = at.split(" ", 1)[0]
    if "(" in at and at.endswith(")"):
        return name, at[at.index("(") + 1:-1]
    return name, ""


def check_arg_rule(stage_no, turn_name, at, arg, data):
    """逐条核 `parse_action_arg()` 的规则（**核不上就抛** —— 规则错了产物就全错）。"""
    name = at.split(" ", 1)[0]
    if len(data) != 1:
        # 只有 `actionOnPlayerResign`（6 关全空）会是 0 条；>1 条没见过 ⇒ 出声
        assert len(data) == 0, "S%d/%s 的 %r 有 %d 条 scriptedActionData（没见过，先看 SO）" % (
            stage_no, turn_name, at, len(data))
        return
    sub, act_type = data[0]["actionType"], data[0]["actingUnitType"]
    if sub in ARG_IS_UNIT or sub == ARG_IS_UNIT_140:
        want = WARLORD_ARG.get(act_type)
        if want is None:
            assert arg != WARLORD_ARG[10] and arg != WARLORD_ARG[20], (
                "S%d/%s %s：actingUnitType=%d 却带着督军显示名 %r" % (stage_no, turn_name, at, act_type, arg))
            if not data[0]["actingUnit"]["m_PathID"]:
                assert arg == "", "S%d/%s %s：actedUnit 为空却带括号 %r" % (stage_no, turn_name, at, arg)
        else:
            assert arg == want, "S%d/%s %s：actingUnitType=%d 期望 %r 实得 %r" % (
                stage_no, turn_name, at, act_type, want, arg)
    elif sub in ARG_IS_TEXT:
        assert arg != "", "S%d/%s %s：聊天/提示类居然没有文案" % (stage_no, turn_name, at)
    else:
        assert arg == "" or name == "PlayCard", "S%d/%s %s：这类动作不该带括号（sub=%d）" % (
            stage_no, turn_name, at, sub)


def strip_snapshot_tags(s):
    """派生快照里那份文案是怎么洗的（实测反推，见 SNAP_STRIP 注释）。"""
    return SNAP_STRIP.sub("", s).replace("<br>", "\n")


# ============================================================ ② pid → 卡名（原版 SO 自己的字符串给的）
def build_pid_name_map(stages):
    """`actingUnit` 的 pid → 卡名。**判据 = SO 自己的 `actionType` 字符串**（不是我们猜的）。

    `UpdateName()` 在"动作类 + 非督军 + ref 非空"时会把 `RawCardScript` 的卡名写进括号 ⇒
    把这些 (pid, 卡名) 收集起来。实测 76 个 pid、**同一个 pid 只对应一个名字**（多值就抛）。
    ⚠️ 覆盖不到的：目标单位（括号里只写发起者）、督军 ref、以及 6 个只在别处出现的 pid。
    """
    m = defaultdict(set)
    for stage in stages:
        for grp in all_groups(stage):
            for a in grp["scriptedActions"]:
                name, arg = parse_action_arg(a["actionType"])
                data = a["scriptedActionData"]
                if not arg or len(data) != 1:
                    continue
                sub = data[0]["actionType"]
                # 🔴 只有 `ARG_IS_UNIT` 那批的括号里才是**单位显示名**：
                #    聊天/提示类（ARG_IS_TEXT）括号里是**整段文案**，混进来会得出一堆假卡名（实测踩过）
                if sub not in ARG_IS_UNIT and sub != ARG_IS_UNIT_140:
                    continue
                if data[0]["actingUnitType"] in WARLORD_ARG:
                    continue  # 督军那半是 "Player warlord"/"Enemy warlord"，不是卡名
                pid = data[0]["actingUnit"]["m_PathID"]
                if pid:
                    m[pid].add(arg)
    bad = {p: v for p, v in m.items() if len(v) > 1}
    assert not bad, "同一个 pid 解出多个卡名（规则不对，别继续）：%s" % bad
    return {p: sorted(v)[0] for p, v in m.items()}


def pid2str(pid):
    """PPtr 的 pid → 字符串（64 位有符号，JS/表格里当**坐标**看，不当数字用）。"""
    return str(pid) if pid else ""


def build_card_resolver(pool):
    """卡名 → 我们的卡 id。同名唯一才算命中；`exact` 优先、不然按 `norm`。"""
    by_exact = defaultdict(list)
    by_norm = defaultdict(list)
    for c in pool:
        by_exact[c["name"]].append(c)
        by_norm[norm(c["name"])].append(c)

    def resolve(name):
        """→ (our id, tier, note)。`tier`：exact / norm / no-hit。**模糊档一律不出结果**（宁可认不出）。"""
        if not name:
            return "", "", ""
        for table, tier in ((by_exact, "exact"), (by_norm, "norm")):
            hits = table.get(name if tier == "exact" else norm(name), [])
            ids = sorted({c["id"] for c in hits})
            if len(ids) == 1:
                return ids[0], tier, ""
            if len(ids) > 1:
                return "", "ambiguous", "池里有 %d 张同名：%s" % (len(ids), ",".join(ids))
        return "", "no-hit", ""
    return resolve


# 🔴 **2026-10-17 裁定（调度台）**：教程 SO 用的名字里，有 2 张**能钉死**到我们池的某一张卡 —— 判据是
#    `数据/游戏数据/card_stats.json` 的 **`ocrName`** 列（= 当年从 **PnP 成品卡图**上 OCR 出来的名字）**逐字就是** SO 那个名字，
#    而且在我们池里**位置唯一**：
#      · `Gauss Warrior (tutorial)` → **`SAU10`**（我们表里叫 `Gauss Reaper Warrior`，其 `ocrName` = `Gauss Warrior`）
#      · `Tesla Immortal`           → **`SAU12`**（我们表里叫 `Tesla Carbine Immortal`，其 `ocrName` = `Tesla Immortal`）
#    ⚠️ **这是按 `card_stats.ocrName` 对上的（判据 = 名字逐字吻合 + 池内唯一），不是原版明确指认** ——
#       原版那份卡 SO（`allcards_assets_all.bundle`）本地没有（见文件头与报告 ⑤），**钉不死**它俩与那两张是同一张卡。
#    产物里**原 SO 名保留**（`name` 字段一个字不改），只在 `ourId` 写我们的 id、`matchTier` = `ocrName`（与 exact/norm 区分开）。
#    ✅ 每次重跑都**当场重验这条判据**（`verify_ocr_fix`）：卡在池里 + `ocrName` 与 SO 名按 `norm` 相等，不成立就抛。
#    ⛔ `Bladeguard Lieutenant` **故意不在这张表里**：`card_stats.json` 里有、我们池里**没有** ⇒ 那是**池缺卡**，
#       另开一件查（硬塞一张相近的卡 = 静默错一张，项目红线）。
OCR_NAME_FIX = {
    "Gauss Warrior (tutorial)": "SAU10",
    "Tesla Immortal": "SAU12",
}


def build_candidate_lookup(stats_path):
    """解不出时给一条**候选线索**（⚠️ 只写进 `note`，⛔ 默认**绝不当结果用** —— 项目红线：宁可认不出）。

    判据 = `数据/游戏数据/card_stats.json` 的 **`ocrName`** 列（= 当年从 PnP 成品卡图上 OCR 出来的名字；
    与 `name`（官方 id 表那套）**可以不同**，实测两个例子：`Gauss Reaper Warrior`↔`Gauss Warrior`、
    `Tesla Carbine Immortal`↔`Tesla Immortal`）。⇒ 教程 SO 用的名字与 `ocrName` 一致、与 `name` 不一致时，
    默认**只记成"候选"**；被调度台裁定采纳的那两条走 `OCR_NAME_FIX`（并由此函数 `verify_ocr_fix` 复核判据）。
    """
    stats = load(stats_path)["cards"]
    by_ocr = defaultdict(set)
    by_name = set()
    ocr_of = {}
    for c in stats:
        if c.get("ocrName"):
            by_ocr[c["ocrName"]].add(c["name"])
        if c.get("name"):
            by_name.add(c["name"])
            ocr_of[c["name"]] = c.get("ocrName") or ""

    def candidate(name):
        """→ 说明字符串（空串 = 没有线索）。"""
        if not name:
            return ""
        same_ocr = {c for c in by_ocr if norm(c) == norm(name)}
        if len(same_ocr) == 1:
            canon = sorted(by_ocr[sorted(same_ocr)[0]])
            return "候选（**未采纳**）：card_stats.json 里 `ocrName=%s` 的卡叫 %s —— 名字与 SO 一致、位置就一个" % (
                sorted(same_ocr)[0], "/".join("「%s」" % c for c in canon))
        if name in by_name:
            return "候选（**未采纳**）：`card_stats.json` 里**有**这张卡（name=%s），但我方卡池 " \
                   "`cards_engine.json` 里没有 ⇒ 池缺卡（不是本批的事）" % name
        return ""

    def verify_ocr_fix(name, cid, pool_name):
        """复核 `OCR_NAME_FIX` 那条裁定的**判据本身**（每次重跑都验，判据塌了就地抛）。"""
        ocr = ocr_of.get(pool_name, "")
        assert ocr, "OCR_NAME_FIX[%s]=%s（%s）在 card_stats.json 里没有 `ocrName` ⇒ 判据不成立" % (name, cid, pool_name)
        assert norm(ocr) == norm(name), \
            "OCR_NAME_FIX[%s]=%s：判据不成立（`ocrName`=%r 与 SO 名对不上）" % (name, cid, ocr)
        return ocr
    return candidate, verify_ocr_fix


# ============================================================ ③ 读原版 bundle（UnityPy）
def _unitypy():
    try:
        import UnityPy
    except ImportError:
        sys.stderr.write(
            "ERROR: 这个脚本需要 UnityPy（要开原版 bundle 解牌组名/音效名）。\n"
            "       请用:  \"D:/2/Warpforge_tools/py312/python.exe\" %s\n" % os.path.abspath(__file__))
        raise
    return UnityPy


def resolve_deck_names(pids):
    """pid → 牌组 SO 资产名（开 `prebuiltdecks_assets_all.bundle`）。**不是按名字猜的**。"""
    UnityPy = _unitypy()
    env = UnityPy.load(BUNDLE_DECKS)
    out = {}
    for o in env.objects:
        if o.path_id in pids and o.type.name == "MonoBehaviour":
            out[o.path_id] = o.read().m_Name
    miss = pids - set(out)
    assert not miss, "这些牌组 pid 在 prebuiltdecks 包里没解出：%s" % sorted(miss)
    return out


def resolve_sound_names(pids):
    """pid → AudioClip 名（7 个包逐个扫；实测 95/95 全解出，~2 秒）。"""
    UnityPy = _unitypy()
    out = {}
    for b in BUNDLES_AUDIO:
        env = UnityPy.load(os.path.join(D_AAB, b))
        for o in env.objects:
            if o.path_id in pids and o.type.name == "AudioClip":
                out[o.path_id] = o.read().m_Name
    assert set(out) == set(pids), "这些音效 clip pid 没解出：%s" % sorted(set(pids) - set(out))
    return out


# ============================================================ ④ 组装
def all_groups(stage):
    """SO 里所有"动作组"：preMulligan + 每回合 + onVictory + onDefeat（顺序固定，便于遍历）。"""
    gs = [stage["preMulliganScriptedActions"]]
    gs += stage["turnScriptedData"]
    gs += [stage["onVictoryScriptedActions"], stage["onDefeatScriptedActions"]]
    return gs


def make_card_ref(ptr, name, resolve):
    """一个 `RawCardScript` 引用 → 产物对象（**无 null**：没有就空串）。"""
    pid = ptr["m_PathID"] if ptr else 0
    if not pid:
        return dict(pid="", name="", ourId="", matchTier="null-ref", note="")
    nm = name or ""
    our, tier, note = resolve(nm) if nm else ("", "no-name", "")
    return dict(pid=pid2str(pid), name=nm, ourId=our, matchTier=tier, note=note)


def make_data_entry(d, pidname, resolve):
    """一条 `ScriptedActionData` → 产物对象（谁打谁）。"""
    def unit(typ, ptr):
        pid = ptr["m_PathID"] if ptr else 0
        if not pid:
            # 压根没有引用（`m_PathID: 0`）—— 与"有 pid 但解不出名字"是两件事，别混成同一个 tier
            return dict(unitType=typ, unitTypeName=ACTION_UNIT.get(typ, "?"),
                        pid="", name="", ourId="", matchTier="null-ref", note="")
        nm = pidname.get(pid, "")
        our, tier, note = resolve(nm) if nm else ("", "no-name", "")
        return dict(unitType=typ, unitTypeName=ACTION_UNIT.get(typ, "?"),
                    pid=pid2str(pid), name=nm, ourId=our, matchTier=tier, note=note)
    t = d["actionType"]
    return dict(type=t, typeName=ACTION_TYPE.get(t, "?"),
                acting=unit(d["actingUnitType"], d["actingUnit"]),
                target=unit(d["targetUnitType"], d["targetUnit"]))


def make_action(i, a, pidname, sounds, resolve):
    """一条 `ScriptedAction` → 产物对象。字段名 → SO 字段是**一对一**的（除了派生的那三个）。"""
    name, arg = parse_action_arg(a["actionType"])
    data = a["scriptedActionData"]
    clip = a["sound"]["clip"]
    cpid = clip["m_PathID"]
    return dict(
        index=i,
        type=name,                      # 派生：scriptedActionData[0].actionType 的枚举名
        text=a["actionType"],           # 原样保留 SO 的字符串（原版拿它进日志）
        arg=arg,                        # 派生：括号里那半（聊天/提示类=文案；动作类=发起者显示名）
        sound=dict(pid=pid2str(cpid), name=sounds.get(cpid, "") if cpid else "",
                   volume=a["sound"]["volume"]),   # ⚠️ volume 原样：0 就是 0，原版自己就这么存的
        waitBefore=a["waitBefore"], waitAfter=a["waitAfter"],
        textReference=a["textReference"], isPCTip=bool(a["isPCTip"]),
        playerAction=bool(a["playerAction"]), shouldHighlightElement=bool(a["shouldHighlightElement"]),
        smallTipParams=dict(
            positionReference=a["smallTipParams"]["positionReference"],
            positionRelation=a["smallTipParams"]["positionRelation"],
            showLeftArrow=bool(a["smallTipParams"]["showLeftArrow"]),
            showRightArrow=bool(a["smallTipParams"]["showRightArrow"]),
            tipDuration=a["smallTipParams"]["tipDuration"],
            waitForTip=bool(a["smallTipParams"]["waitForTip"]),
            tipWithContinue=bool(a["smallTipParams"]["tipWithContinue"]),
            preventSkipTip=bool(a["smallTipParams"]["preventSkipTip"])),
        data=[make_data_entry(x, pidname, resolve) for x in data],
    )


def make_group(g, pidname, sounds, resolve):
    """一个 `TurnScriptedData` → 产物对象（组内动作**保持原序** = 教学脚本赖以成立的东西）。"""
    acts = [make_action(i, a, pidname, sounds, resolve)
            for i, a in enumerate(g["scriptedActions"])]
    return dict(turnName=g["turnName"], scriptedTurn=bool(g["scriptedTurn"]),
                dontDrawCard=bool(g["dontDrawCard"]), dontDrawTalent=bool(g["dontDrawTalent"]),
                actionCount=len(acts), actions=acts)


def make_starting_list(items, pidname, resolve):
    """`playerStartingTroops` 这类 `List<RawCardScript>` → 产物对象数组。"""
    return [make_card_ref(p, pidname.get(p["m_PathID"], ""), resolve) if p["m_PathID"]
            else dict(pid="", name="", ourId="", matchTier="null-ref", note="") for p in items]


# ============================================================ ⑤ 交叉校验（对照派生快照）
def cross_check(stages, snapshot):
    """本产物 ↔ `数据/游戏数据/tutorial_stages.json` 逐回合对账。

    **那条快照的 `actions[]` = SO 里 `playerAction==true` 的那些（按原序）、`tips[]` = SmallTip 的文案
    （洗掉 `<link>`/`<nobr>`/`<sprite>`、保留 `<b>`、`<br>`→换行）** —— 这几条都是本函数**实测反推**的规则，
    对得上才说明"两边读的是同一份 SO"（B6 已验过 79/79，本脚本每次重跑再验一遍）。
    ⚠️ 快照的 `turn` 字段 79 条**全是 "1"**（原型那个管道的口径，与 SO 的回合序号无关）⇒ 拿不了它对账。
    """
    lines, bad = [], []
    for i, stage in enumerate(stages, 1):
        snap = snapshot["Stage%d" % i]["steps"]
        turns = stage["turnScriptedData"]
        n_bad = 0
        assert len(snap) == len(turns), "Stage%d：快照 %d 回合 vs SO %d 回合" % (i, len(snap), len(turns))
        for si, (st, t) in enumerate(zip(snap, turns)):
            acts = [a["actionType"].split(" ", 1)[0] for a in t["scriptedActions"] if a["playerAction"]]
            tips = [strip_snapshot_tags(parse_action_arg(a["actionType"])[1])
                    for a in t["scriptedActions"] if parse_action_arg(a["actionType"])[0] == "SmallTip"]
            ok = (st["actions"] == acts and st["tips"] == tips
                  and st["name"] == t["turnName"]
                  and bool(st["is_player"]) == t["turnName"].startswith("Player"))
            if not ok:
                n_bad += 1
                bad.append("Stage%d step%d：SO 动作 %s / 快照 %s；SO tip %d 条 / 快照 %d 条"
                           % (i, si + 1, acts, st["actions"], len(tips), len(st["tips"])))
        lines.append("Stage%d: %d 回合 / 玩家动作 %d / tip %d / 不符 %d"
                     % (i, len(turns),
                        sum(1 for t in turns for a in t["scriptedActions"] if a["playerAction"]),
                        sum(1 for t in turns for a in t["scriptedActions"]
                            if parse_action_arg(a["actionType"])[0] == "SmallTip"),
                        n_bad))
    return lines, bad


def assert_jsonable(o, path="doc"):
    """产物必须让 `JsonUtility` 读得动：**只有 string / 数字 / bool / 数组 / 嵌套对象**，无字典、无 None。"""
    if isinstance(o, dict):
        assert all(isinstance(k, str) for k in o), path + " 有非字符串键"
        for k, v in o.items():
            assert_jsonable(v, path + "." + k)
    elif isinstance(o, list):
        for i, v in enumerate(o):
            assert_jsonable(v, "%s[%d]" % (path, i))
    else:
        assert isinstance(o, (str, int, float, bool)) and o is not None, \
            "%s = %r（JsonUtility 读不动的类型）" % (path, o)
        if isinstance(o, float):
            # `json.dump` 会把 NaN/Infinity 写成裸字面量，而 `JsonUtility` 读不了 ⇒ 这里先拦下来
            assert o == o and o not in (float("inf"), float("-inf")), "%s = %r（非有限数）" % (path, o)


# ============================================================ 主流程
def main():
    stages_raw, so_sha = [], []
    for n in STAGE_NAMES:
        p = os.path.join(D_SO, n + ".json")
        with open(p, "rb") as f:
            so_sha.append(hashlib.sha1(f.read()).hexdigest())
        stages_raw.append(load(p))
    assert [s["m_Name"] for s in stages_raw] == STAGE_NAMES, "SO 的 m_Name 与文件名对不上"

    pool = load(POOL)["cards"]
    pool_by_id = {c["id"]: c for c in pool}
    _resolve0 = build_card_resolver(pool)
    _candidate, _verify_ocr = build_candidate_lookup(STATS)

    def resolve(name):
        """（我们的卡 id, tier, note）—— 先按名字解；解不出再看**裁定表** `OCR_NAME_FIX`；再不行挂候选线索（⛔ 不改结果）。"""
        our, tier, note = _resolve0(name)
        if our:
            return our, tier, note
        fix = OCR_NAME_FIX.get(name)
        if fix:
            c = pool_by_id.get(fix)
            assert c is not None, "OCR_NAME_FIX[%r] = %s 不在卡池里" % (name, fix)
            ocr = _verify_ocr(name, fix, c["name"])
            return fix, "ocrName", ("🔴 按裁定映射到 %s（池里叫「%s」）：**判据 = card_stats.json 的 `ocrName`=%r 与 SO 名逐字吻合 + "
                                    "池内唯一，不是原版明确指认**（原版卡 SO 包 `allcards_assets_all.bundle` 本地没有，钉不死）"
                                    % (fix, c["name"], ocr))
        if not note:
            note = _candidate(name)
        return our, tier, note

    snapshot = load(SNAPSHOT)
    tut_decks = load(TUT_DECKS)

    # ---- pid → 卡名（SO 自己的字符串给的）----------------------------------
    pidname = build_pid_name_map(stages_raw)
    print("pid->卡名：%d 个（判据 = SO 自己的 actionType 字符串）" % len(pidname))

    # ---- 牌组 / 音效：开原版 bundle 解 -------------------------------------
    deck_pids = set()
    for s in stages_raw:
        for k in ("playerDeck", "aiDeck"):
            deck_pids.add(s[k]["m_PathID"])
    assert 0 not in deck_pids and len(deck_pids) == 12, "牌组 PPtr 应是 12 个不同的 pid：%s" % sorted(deck_pids)
    deck_names = resolve_deck_names(deck_pids)
    sound_pids = set()
    for s in stages_raw:
        for g in all_groups(s):
            for a in g["scriptedActions"] + [s["actionOnPlayerResign"]]:
                if a["sound"]["clip"]["m_PathID"]:
                    sound_pids.add(a["sound"]["clip"]["m_PathID"])
    sounds = resolve_sound_names(sound_pids)
    print("牌组名 %d/12 · 音效名 %d/%d（都开原版 bundle 解的）"
          % (len(deck_names), len(sounds), len(sound_pids)))

    # ---- 逐关组装 ----------------------------------------------------------
    # 没解出来的引用：**按 pid 归并**（同一个 pid 会用很多次，逐条刷 33 行没人看），
    # 并分两类 —— ① 名字都拿不到的（原版卡资产包本地没有）② 有名字但我们的池里没有
    seen_ref = {}

    def note_ref(where, r):
        if not r["pid"] or r["ourId"]:
            return
        e = seen_ref.setdefault((r["pid"], r["matchTier"]), dict(name=r["name"], note=r["note"], n=0, sites=[]))
        e["n"] += 1
        if len(e["sites"]) < 3:
            e["sites"].append(where)

    stages_out = []
    for i, s in enumerate(stages_raw, 1):
        # 每条动作先按规则核一遍（核不上就炸，别产出错的）
        for g in all_groups(s):
            for a in g["scriptedActions"]:
                check_arg_rule(i, g["turnName"], a["actionType"], parse_action_arg(a["actionType"])[1],
                               a["scriptedActionData"])
        # 牌组名 ↔ B6 产物（`tutorial_decks.json` 的 `so`）交叉校验：两边独立，对得上说明都对
        for role, key in (("player", "playerDeck"), ("ai", "aiDeck")):
            got = deck_names[s[key]["m_PathID"]]
            want = tut_decks["stages"][i - 1][role]["so"]
            assert got == want, ("Stage%d %s 的牌组：SO pid 解出 %r，但 tutorial_decks.json 写的是 %r"
                                 % (i, role, got, want))
        turns = [make_group(g, pidname, sounds, resolve) for g in s["turnScriptedData"]]
        stage = dict(
            stage=i, so=s["m_Name"], soSha1=so_sha[i - 1],
            uniqueIdCampaign=s["uniqueIdCampaign"], bg=s["bg"],
            playerStarts=bool(s["playerStarts"]),
            hideCemetery=bool(s["hideCemetery"]), hideCardsLeftInDeck=bool(s["hideCardsLeftInDeck"]),
            hideLargeCardDisplay=bool(s["hideLargeCardDisplay"]), hideChat=bool(s["hideChat"]),
            playerAlwaysWins=bool(s["playerAlwaysWins"]),
            preventPlayerResign=bool(s["preventPlayerResign"]),
            skipNormalBattleEndOnVictory=bool(s["skipNormalBattleEndOnVictory"]),
            skipNormalBattleEndOnDefeat=bool(s["skipNormalBattleEndOnDefeat"]),
            startingPlayerMana=s["startingPlayerMana"], startingEnemyMana=s["startingEnemyMana"],
            startingPlayerDamage=s["startingPlayerDamage"], startingEnemyDamage=s["startingEnemyDamage"],
            playerDeck=deck_names[s["playerDeck"]["m_PathID"]],
            playerDeckPid=pid2str(s["playerDeck"]["m_PathID"]),
            aiDeck=deck_names[s["aiDeck"]["m_PathID"]],
            aiDeckPid=pid2str(s["aiDeck"]["m_PathID"]),
            playerDeckForScenarios=make_card_ref(s["playerDeckForScenarios"], "", resolve),
            playerStartingTroops=make_starting_list(s["playerStartingTroops"], pidname, resolve),
            enemyStartingTroops=make_starting_list(s["enemyStartingTroops"], pidname, resolve),
            playerStartingTroopsInHand=make_starting_list(s["playerStartingTroopsInHand"], pidname, resolve),
            enemyStartingTroopsInHand=make_starting_list(s["enemyStartingTroopsInHand"], pidname, resolve),
            playerTurnEndEnchantment=make_card_ref(s["playerTurnEndEnchantment"], "", resolve),
            enemyTurnEndEnchantment=make_card_ref(s["enemyTurnEndEnchantment"], "", resolve),
            preMulligan=make_group(s["preMulliganScriptedActions"], pidname, sounds, resolve),
            onVictory=make_group(s["onVictoryScriptedActions"], pidname, sounds, resolve),
            onDefeat=make_group(s["onDefeatScriptedActions"], pidname, sounds, resolve),
            actionOnPlayerResign=make_action(-1, s["actionOnPlayerResign"], pidname, sounds, resolve),
            turnCount=len(turns), turns=turns,
        )
        stages_out.append(stage)

        # 没解出来的引用：逐个记（**不许静默**）
        for k in ("playerStartingTroops", "enemyStartingTroops",
                  "playerStartingTroopsInHand", "enemyStartingTroopsInHand"):
            for j, r in enumerate(stage[k]):
                note_ref("S%d %s[%d]" % (i, k, j), r)
        for grp in [stage["preMulligan"]] + stage["turns"] + [stage["onVictory"], stage["onDefeat"]]:
            for a in grp["actions"]:
                for e in a["data"]:
                    for side in ("acting", "target"):
                        note_ref("S%d %s 第%d 条 %s" % (i, grp["turnName"], a["index"], a["type"]),
                                 e[side])

    # ---- 交叉校验 ----------------------------------------------------------
    xc_lines, xc_bad = cross_check(stages_raw, snapshot)
    assert not xc_bad, "与派生快照对不上（说明两边读的不是同一份 SO，先查）：\n  " + "\n  ".join(xc_bad[:8])

    def tally(groups):
        """→ (动作数, playerAction 数, SmallTip 数)。"""
        return (sum(g["actionCount"] for g in groups),
                sum(1 for g in groups for a in g["actions"] if a["playerAction"]),
                sum(1 for g in groups for a in g["actions"] if a["type"] == "SmallTip"))

    def groups_of(s):
        return [s["preMulligan"]] + s["turns"] + [s["onVictory"], s["onDefeat"]]

    n_turn = sum(s["turnCount"] for s in stages_out)
    n_act, n_pa, n_tip = tally([g for s in stages_out for g in groups_of(s)])
    turn_act, turn_pa, turn_tip = tally([g for s in stages_out for g in s["turns"]])
    pre_act, pre_pa, pre_tip = tally([s["preMulligan"] for s in stages_out])
    vic_act, vic_pa, vic_tip = tally([s["onVictory"] for s in stages_out])
    # 🔴 这几个数是**上一批查实的**（回合内 430 条动作 / 152 条玩家动作 / 61 条 tip），对不上先别信产物。
    #    ⚠️ 上一批那个 430 **只算回合内**；本产物的 `actionCount` 是**全量**（回合 + preMulligan + onVictory + onDefeat）。
    assert (n_turn, turn_act, turn_pa, turn_tip) == (79, 430, 152, 61), \
        "回合内的规模与上一批查实的不符：回合%d 动作%d 玩家动作%d tip%d" % (n_turn, turn_act, turn_pa, turn_tip)

    # ---- 没解出的引用 → 两条清单（分两类，按 pid 归并）+ knownIssues -------------
    def fmt(pid_tier, e):
        pid, tier = pid_tier
        return ("pid=%s  tier=%s  名字=%s  出现 %d 处（%s%s）%s"
                % (pid, tier, ("「%s」" % e["name"]) if e["name"] else "（没有）", e["n"],
                   "、".join(e["sites"]), " 等" if e["n"] > len(e["sites"]) else "",
                   "；" + e["note"] if e["note"] else ""))

    no_name = sorted([k for k in seen_ref if k[1] == "no-name"])
    not_pool = sorted([k for k in seen_ref if k[1] in ("no-hit", "ambiguous")])
    unresolved_no_name = [fmt(k, seen_ref[k]) for k in no_name]
    unresolved_not_pool = [fmt(k, seen_ref[k]) for k in not_pool]

    # 解出情况的一张直方图（自己看得见才有底）：`tier` → 出现了多少次
    hist = Counter()
    for s in stages_out:
        for g in [s["preMulligan"]] + s["turns"] + [s["onVictory"], s["onDefeat"]]:
            for a in g["actions"]:
                for e in a["data"]:
                    for side in ("acting", "target"):
                        hist[e[side]["matchTier"]] += 1
        for k in ("playerStartingTroops", "enemyStartingTroops",
                  "playerStartingTroopsInHand", "enemyStartingTroopsInHand"):
            for r in s[k]:
                hist["起始单位:" + r["matchTier"]] += 1
    histLine = " · ".join("%s=%d" % (k, v) for k, v in sorted(hist.items()))

    # `OCR_NAME_FIX` 那两条裁定：每次重跑都**独立再验一遍判据**（即使这次没人用到它们），并把映射原样印进产物
    ocr_fix_lines = []
    for nm, cid in sorted(OCR_NAME_FIX.items()):
        c = pool_by_id[cid]
        ocr = _verify_ocr(nm, cid, c["name"])
        ocr_fix_lines.append("「%s」(= SO 原名，产物 `name` 字段**原样保留**) → `%s`（我们卡池里叫「%s」，"
                             "其 `card_stats.json` 的 `ocrName` = %r）" % (nm, cid, c["name"], ocr))

    known = [
        "`actionOnPlayerResign` 6 关**全是空动作**（`actionType` 空串）⇒ 与 `preventPlayerResign` 全 0 自洽"
        "（原版原文如此，⛔ 别当缺失去补）",
        "🔴 有 %d 个 pid **连名字都拿不到**（`unresolvedNoName`）：它们是 `scriptedActionData` 里引用的卡 SO，"
        "住在 **`allcards_assets_all.bundle`**（= SO externals 的第 3 项 `CAB-fb8f01449289635703d83d52c3f18245`）——"
        "那个包**在 catalog 里有、本地 84 个 bundle 里没有**（同缺的还有 `alternateartstyles`/`localization`）"
        "⇒ 解不出名字。⚠️ 名字的**唯一**来源是 SO 自己的 `actionType` 字符串（只写发起单位），"
        "所以「只当目标」的那些 pid 天然没名字（督军那几种由 `unitType` 就够用了，不需要卡名）" % len(no_name),
        "🔴 有 %d 张卡**有名字但我们的池里没有**（`unresolvedNotPool`）—— **没有猜**：`ourId` 留空、"
        "线索写进该 ref 的 `note`（= `card_stats.json` 的 `ocrName` 列）。要落地得像别的账一样先裁定" % len(not_pool),
        "🔴 %d 张卡走了**裁定映射**（`ocrNameFixes`）：**判据 = `card_stats.json` 的 `ocrName` 列与 SO 名逐字吻合 + 池内唯一，"
        "不是原版明确指认**（原版卡 SO 包本地没有 ⇒ 钉不死）。产物里 **SO 原名一字未改**（`name`），只在 `ourId` 写我们的 id、"
        "`matchTier` = `ocrName`（与 exact/norm 分开，别当「名字对上了」）" % len(ocr_fix_lines),
        "`sound.volume` 原样保留（SO 序列化那一层）：多数是 0.0（82/95），少数 1.0。"
        "🔴 **2026-10-18 就地订正（铁律 5）**：本行原文写着「`BattleManager.PlayNextSoundInQueue.c` 是 "
        "`PlayOneShot(clip, AudioListener.volume * sound.volume)` ⇒ **0 就是静音**」——**后半句是错的**："
        "运行时读的那支音量**不是** SO 里这一格，而是 `ScriptedActionCampaignData__GetSoundAsset.c` "
        "新建 `SoundAsset` 时**硬编码写死的 `0x3f800000`（= 1.0）**（`*(lVar2 + 0x18) = 0x3f800000`）。"
        "SO 那一格**在运行时根本不被采信** ⇒ 0.0 的那些**照样出声**（`PlayOneShot(clip, AudioListener.volume * 1.0)`）。"
        "⛔ 所以「别顺手修成 1.0」那句要反过来理解：**播放侧就该按 1.0**（照 `GetSoundAsset`），"
        "而产物里这一格留着只作**记账/对账**。",
        "教程文案的**本地化表在远端**（`textReference` = `Tutorial1/Turn1/Tip1` 这类键，84 个 bundle 里没有语言表）"
        "⇒ 只能沿用 SO 内嵌的英文（`arg` 字段就是它）",
        "教程关卡的**第一句对白/提示的时序**：本产物把 `preMulligan` 与每回合的动作分成独立数组、"
        "**组内保持原序**（tip 就是组里的一条 `SmallTip` 动作）⇒ 「第几条 tip 在第几个动作之前」这层信息在，别重排",
    ]

    doc = dict(
        format=FORMAT,
        note="由 工具/gen_tutorial_stages.py 生成，**别手改**。数据源 = 原版 6 个 TutorialStage SO"
             "（`bundle_tutorialso_assets_all`）。🔴 这是**全量**（462 条动作 = 回合内 430 + preMulligan 19 + onVictory 13）；"
             "`数据/游戏数据/tutorial_stages.json` 是 Godot 原型的**派生快照**（只有 152 条、且 tip 的时序没了）"
             "—— **两份文件名字恰好相同**，⛔ 别混（那份不能当运行时数据源，出处见 report）。"
             "⚠️ 有 %d 张卡是按 `card_stats.json` 的 `ocrName` **裁定映射**到我们卡池的（**不是原版明确指认**）"
             "—— 判据与保留原名的约定见 `ocrNameFixes`/`ocrNameFixNote`。" % len(ocr_fix_lines),
        source="d:/2/新解包资源/assets_full/bundle_tutorialso_assets_all/MonoBehaviour/Warpforge_TutorialStage{1..6}.json",
        generatedFrom="原版 6 个 SO（sha1 见每关 `soSha1`）+ 原版 bundle 查牌组名/音效名 + cards_engine.json 查卡 id",
        fieldSource=[
            "playerDeck/aiDeck = SO 的 `playerDeck`/`aiDeck`（PPtr → 按 pid 开 prebuiltdecks 包解成资产名；`*Pid` 留坐标）",
            "playerStarts = `playerStarts`（S3 是 0 ⇒ AI 先手）",
            "startingPlayerMana/EnemyMana/PlayerDamage/EnemyDamage = 同名四个 int（6 关全 0）",
            "hideCemetery/hideCardsLeftInDeck/hideLargeCardDisplay/hideChat = 同名四个 bool",
            "playerAlwaysWins/preventPlayerResign/skipNormalBattleEndOnVictory/skipNormalBattleEndOnDefeat = 同名四个 bool",
            "{player,enemy}StartingTroops{InHand} = 同名四个 `List<RawCardScript>`（pid → 名字见 `name`；"
            "名字来自 SO 自己的 actionType 字符串，见 gen_tutorial_stages.py 的 parse_action_arg）",
            "preMulligan/onVictory/onDefeat/turns = `preMulliganScriptedActions`/`onVictoryScriptedActions`/"
            "`onDefeatScriptedActions`/`turnScriptedData`；动作组内**原序**",
            "每个动作：type=scriptedActionData[0].actionType 的枚举名（**运行时读的就是它**）· text=SO 的 `actionType` 原串 · "
            "arg=括号里那半 · sound/waitBefore/waitAfter/textReference/isPCTip/playerAction/shouldHighlightElement/"
            "smallTipParams = 同名 SO 字段（一对一的）",
            "每条 ScriptedActionData：type/typeName（`ScriptedActionType` 枚举）+ acting/target"
            "（unitType/unitTypeName=`ScriptedActionUnit` 枚举 · pid · name · ourId · matchTier）",
        ],
        enumScriptedActionType=["%d=%s" % (k, v) for k, v in sorted(ACTION_TYPE.items())],
        enumScriptedActionUnit=["%d=%s" % (k, v) for k, v in sorted(ACTION_UNIT.items())],
        enumPositionReference=["%d=%s" % (k, v) for k, v in sorted(POSITION_REFERENCE.items())],
        enumPositionRelation=["%d=%s" % (k, v) for k, v in sorted(POSITION_RELATION.items())],
        actionTypeNote="SO 的 `actionType` 是**显示名**（`ScriptedAction.UpdateName()` 生成），"
                       "只进一句 `CustomDebug.Log`（`AiScripted__ExecuteAction.c:59-67`）；"
                       "**运行时一律读 `scriptedActionData[0].actionType`（枚举 int）** ⇒ 括号里那半**不需要解析**。"
                       "括号内容 = 聊天/提示类的**文案**、动作类的**发起者显示名**（`Player warlord`/`Enemy warlord`/卡名）",
        crossCheck="本产物的 `playerAction==true` 子集 与 派生快照 `actions[]` **逐条相等**；tip 文案（洗掉 link/nobr/sprite、保留 <b>）"
                   "与 `tips[]` 逐条相等；`turnName` ↔ `name`、`is_player` ↔ 名字前缀 全等 ⇒ 两边读的是同一份 SO",
        crossCheckByStage=xc_lines,
        crossCheckNote="⚠️ 快照的 `turn` 字段 79 条**全是 \"1\"**（原型那管道的口径）⇒ 它跟 SO 的回合序号对不上，没拿它对账",
        unresolvedNoName=unresolved_no_name,
        unresolvedNotPool=unresolved_not_pool,
        ocrNameFixes=ocr_fix_lines,
        ocrNameFixNote="🔴 **这 %d 条是【裁定映射】，不是原版明确指认**：判据 = 教程 SO 用的那个名字（= 原版卡 SO 的 `cardName`）"
                       "与 `数据/游戏数据/card_stats.json` 的 **`ocrName`** 列（当年从 **PnP 成品卡图**上 OCR 出来的名字）"
                       "**逐字吻合**、且在我们卡池里**位置唯一**。⚠️ 原版那份卡 SO（`allcards_assets_all.bundle`）本地没有 ⇒ "
                       "**钉不死**它俩与映射到的那两张是同一张卡（数值可能被教程改过）。产物里 **SO 原名原样保留**（`name` 字段），"
                       "只在 `ourId` 写我们的 id、`matchTier` 写 `ocrName`（⛔ 别当 exact/norm 用）。裁定：调度台 2026-10-17。"
                       % len(ocr_fix_lines),
        refNote="`pid` 是原版 SO 里 `RawCardScript` 的 PPtr（64 位有符号，当**坐标**用）；"
                "`name` 来自原版 SO 自己的 `actionType` 字符串（**一个字都不改**，见 gen_tutorial_stages.py 的 parse_action_arg）；"
                "`ourId` 是 `cards_engine.json` 里的卡 id，`matchTier` = exact / norm / ocrName（**不做模糊匹配**，宁可认不出；"
                "`ocrName` = 走了裁定映射，见 `ocrNameFixes`）；`note` 只在解不出时给候选线索（⛔ 候选不当结果用）",
        matchTierHistogram=histLine,
        knownIssues=known,
        stageCount=len(stages_out), turnCount=n_turn,
        actionCount=n_act, playerActionCount=n_pa, tipCount=n_tip,
        turnActionCount=turn_act, turnPlayerActionCount=turn_pa, turnTipCount=turn_tip,
        preMulliganActionCount=pre_act, onVictoryActionCount=vic_act, onDefeatActionCount=0,
        countNote="⚠️ `actionCount`/%d 是**全量**（回合 %d + preMulligan %d + onVictory %d + onDefeat 0）；"
                  "上一批查实的那个 **430** 只算**回合内**（= `turnActionCount`）—— 两个数别混。"
                  % (n_act, turn_act, pre_act, vic_act),
        stages=stages_out,
    )
    assert_jsonable(doc)
    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        json.dump(doc, f, ensure_ascii=False, indent=1)

    print("wrote", OUT)
    print("stages=%d turns=%d actions=%d(回合内%d) playerActions=%d(回合内%d) tips=%d(回合内%d) 未解出pid=%d/%d"
          % (len(stages_out), n_turn, n_act, turn_act, n_pa, turn_pa, n_tip, turn_tip,
             len(no_name), len(not_pool)))
    for l in xc_lines:
        print("   " + l)
    print("   tier: " + histLine)
    for u in unresolved_not_pool:
        print("   ! " + u.encode("ascii", "backslashreplace").decode("ascii"))
    for u in unresolved_no_name[:3]:
        print("   ~ " + u.encode("ascii", "backslashreplace").decode("ascii"))
    if len(unresolved_no_name) > 3:
        print("   ~ ...（共 %d 个 pid 没名字，全在产物 unresolvedNoName）" % len(unresolved_no_name))


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
