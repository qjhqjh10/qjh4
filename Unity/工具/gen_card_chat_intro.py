#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
gen_card_chat_intro.py —— 「定制开场白」两档的数据侧（账 `A1754`/`A1805` 的后续；战斗线 T17b）

## 这是什么

原版的战将开场白有**三档**，选择顺序（**反编译逐句实读，唯一口径**）：

```
ChatManager.GetChatMessage(idx, isPlayer)                    // ChatManager__GetChatMessage.c
  if idx < 2 (ChatMessage.Intro=0 / MirrorMatch=1):
     若 (不是同督军) 或 isPlayer != 0:
        ① 精确档 GetCustomIntro(me.warlord, foe.warlord)      // ChatManager__GetCustomIntro.c
             me 若带 customIntroReferenceId ⇒ 先重定向到
             CollectionManager.GetRawCardFromId(那个 id) 那张卡，再遍历它的
             customIntroChatData（+0x1E8）
             命中条件： entry.opponentWarlord(+0x10) == RawCardScript.GetChatId(foe, true)
           → RawCardScript.GetLocalizedCustomIntroChatMsg(me, idx, foe)   // .c
             = "Chat_Messages/" + <me chatId> + "-Chat" + idx + "-" + <foe chatId>
        ② 按阵营档（精确档没命中时）
             GetCustomIntroByArmy(me, foe.army)                // ChatManager__GetCustomIntroByArmy.c
               遍历 me.customIntroByArmyChatData（+0x1F0），entry.army(+0x10) == foe.army
           → RawCardScript.GetLocalizedCustomIntroChatMsgByArmy(me, idx, foe.army)  // .c
             = "Chat_Messages/" + <me chatId> + "-Chat" + idx + "-" + <CardArmy 枚举名>
        ③ 兜底档（两档都没命中）
             RawCardScript.GetLocalizedChatMsg(me, idx)         // .c
             = "Chat_Messages/" + <chatMessagesReferenceId ?: uniqueId> + "-Chat" + idx
             （⚠️ 这一档**不看** customIntroReferenceId）
  else → 常规 Chat<idx>（同一套兜底拼法）
```

`chatId` 的取法（`RawCardScript__GetChatId.c`，字段偏移）：
`chatMessagesReferenceId(+0x168)` 非空就用它；否则（当 `param_2 != 0` 时）`customIntroReferenceId(+0x170)`
非空就用它；否则 `uniqueId(+0x38)`。
两个 CustomIntro 的拼键方法体**自己内联了同一条链**（`0x168 ?: 0x170 ?: 0x38`），
`GetLocalizedChatMsg` 只走 `0x168 ?: 0x38`。

`talksFirst`（谁先说）—— `ChatManager__IsPlayerFirstIntro.c`：
  · 只查**精确档** `GetCustomIntro`，命中就返回 `entry.talksFirst(+0x20)`；
  · **没命中（含只命中「按阵营档」）⇒ 返回 1（玩家先说）**。
  ⇒ `talksFirst` 是**逐对手**的（本表 57 张精确档卡里 31 张逐条不同），不是每卡一个标量。

`-vs*`（`Chat_Messages/<id>-vs<名字>`）：官方表里有 **18 条**，但当前 build 的
`stringliteral.json` 里**没有 `-vs` 字面量**，反编译里也没有任何方法拼它
⇒ **本表不建**（旧版本遗留数据）。

## 真值从哪来

`d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json` —— 当前 build（2026-10-18）的
**1,351 张 RawCardScript** 全字段语料。本表读这四项：
  · `customIntroChatData`（57 张）/ `customIntroByArmyChatData`（15 张）
  · `chatMessagesReferenceId`（8）/ `customIntroReferenceId`（7）
  · 以及每条精确档里的 `talksFirst`。
其中 **60 张**至少带一项；**49 张**的 `uniqueId` 在 `cards_engine.json`（= 运行时卡池）里，
另 **11 张**是**副本 / 教程 SO**（GUID uniqueId），不在卡池 ⇒ 对局取不到（标 `inRuntime=false`，只进全量件）。

⚠️ **重复 SO**（同一张卡两份 uniqueId，短 id 那份才是卡池里的）：副本那份的
`chatMessagesReferenceId`/`customIntroReferenceId` 常常指回短 id（例：`52520927…` → `GOF29`），
数据与短 id 那份**逐字节相同**（Nemesor Zahndrekh / Uriel Ventris (tutorial) 除外，见报告）。
本表**两张都收**（全量件）；运行时件只收 `inRuntime=true`。

## 产物（同一次运行写两份 ⇒ 不会分叉）

1. `d:/4/Unity/数据/游戏数据/card_chat_intro.json` —— **全量真值**（60 张 + 缺键清单 + 算法出处）
2. `d:/4/Unity/MyGame/Assets/CardPresentation/Resources/card_chat_intro.json` —— **运行时件**
   （只收 49 张运行时卡；`Resources.Load<TextAsset>("card_chat_intro")`）

🆕 **2026-10-20（战斗线 `T18`）**：每条开场白多导两格 —— `soundGuid`（SO 的
`customSound.m_AssetGUID`）与 `soundAsset`（用 `资料/普查产出_1018/cardanim_guid_map/
guid_map_bundles.tsv` 解出的 AudioClip 资产名；解不出 ⇒ 空串 + 全量件 `unresolvedSoundGuids`，
**⛔ 不猜成默认音**）。运行时按 `Resources/Art/audio/vo/<soundAsset>` 加载（语音线导入器已把
卡包的 1,857 条 AudioClip 全拷到那里）。

## 用法

    python -I -X utf8 d:/4/Unity/工具/gen_card_chat_intro.py            # 重出两份产物
    python -I -X utf8 d:/4/Unity/工具/gen_card_chat_intro.py --check    # 只比不写（幂等自检）

⛔ 不读写 `d:/2`（只读上面那两份 JSON + 卡池 + 本地 `guid_map*.tsv`）；⛔ 不碰 `cards_engine.json`；
⛔ 缺文案的键**照原版算法拼出来但标 `hasText=false`**，**不自译、不补假键**。
"""

import csv
import io
import json
import os
import re
import sys

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：python 工具/gen_card_chat_intro.py [--check]\n  说明：gen_card_chat_intro.py —— 「定制开场白」两档的数据侧（账 `A1754`/`A1805` 的后续；战斗线 T17b）\n  选项：--check（脚本内按字符串匹配；一个都不带 = 默认模式）')
    _wf_help_sys.exit(0)

ROOT4 = "d:/4/Unity"

CARDS_RAW = ROOT4 + "/资料/普查产出_1018/allcards_1350/cards_raw.json"
LOC_ZH_EN = ROOT4 + "/数据/游戏数据/官方本地化_en+zh-CN.json"
CARDS_ENGINE = ROOT4 + "/MyGame/Assets/RuleEngine/Resources/cards_engine.json"

# ---- `customSound` 的 `AssetReference GUID → 资产名` 对照表 ----
# 🆕 2026-10-20（战斗线 `T18`）：开场白每条的 `customSound` 是 Unity Addressables 的
#   `AssetReferenceTyped<AudioClip>`（`CustomIntroChatData.cs:10` / `CustomIntroByArmyChatData.cs:10`），
#   运行时原版走 `ChatManager.GetChatSoundAsset` → `AssetReference.Load`（`ChatManager__GetChatSoundAsset.c`）。
#   我们按 **`Resources/Art/audio/vo/<资产名>`** 加载（语音线导入器把卡包里的 AudioClip 全拷到了那里）。
#   ⚠️ 两张表都要读：
#     · `guid_map_bundles.tsv`（2026-10-18 直接枚举本地束的容器键建的）—— **190/190 命中**；
#     · 旧 `guid_map.tsv`（2026-10-15 从 Addressables 目录出发）—— **0/190 命中**（音频不在那份目录里）。
#   ⇒ 以束枚举表为主、旧表只作兜底；两表都查不到 ⇒ 落 `unresolvedSoundGuids` 并 **出声**（⛔ 不猜成默认音）。
GUID_MAP_BUNDLES = ROOT4 + "/资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv"
GUID_MAP_OLD = ROOT4 + "/资料/普查产出_第二十七会话/guid_map.tsv"

OUT_FULL = ROOT4 + "/数据/游戏数据/card_chat_intro.json"
OUT_RT = ROOT4 + "/MyGame/Assets/CardPresentation/Resources/card_chat_intro.json"

# CardArmy 枚举（判据 = d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardArmy.cs，
#   与 allcards 的 customIntroByArmyChatData[*].army **现读 29 条全部命中**；
#   ⚠️ 不是 CardArmyOrdered —— 那张表 SaimHann=55，而数据里 SaimHann 写的是 30）。
CARD_ARMY = {
    0: "Neutral",
    10: "Ultramarines",
    20: "Goff",
    30: "SaimHann",
    40: "Sautekh",
    50: "BlackLegion",
    60: "Leviathan",
    70: "TauEmpire",
    80: "Sororitas",
    90: "Genestealers",
    100: "AstraMilitarum",
    110: "DarkAngels",
    120: "EmperorsChildren",
    130: "SpaceWolves",
}

CHAT_PREFIX = "Chat_Messages/"
# ChatMessage.Intro = 0（d:/2/Warpforge_code/Scripts/Assembly-CSharp/ChatMessage.cs）
INTRO_IDX = 0


def _read_json(p):
    with io.open(p, encoding="utf-8") as fh:
        return json.load(fh)


def _ne(v):
    """非空判据（空串 / None / 空 list 都算空）。"""
    if v is None:
        return False
    if isinstance(v, (list, dict)):
        return len(v) > 0
    if isinstance(v, str):
        return v.strip() != ""
    return True


def _sound_guid(entry):
    cs = entry.get("customSound") or {}
    return cs.get("m_AssetGUID") or ""


def _read_guid_map(path, source):
    """`guid_map*.tsv` → {assetGuid: {"name":…, "type":…, "bundle":…, "source":…}}。

    ⚠️ 同一 GUID 可能有多行（旧表里 Texture2D/Sprite 各一行）⇒ **只认 AudioClip**；
    同一 GUID 的多条 AudioClip 若名字不同 ⇒ 出声（本项目「宁可认不出，不可认错」）。
    """
    out = {}
    if not os.path.exists(path):
        return out, "缺失: %s" % path
    with io.open(path, encoding="utf-8") as fh:
        for row in csv.DictReader(fh, delimiter="\t"):
            if row.get("type") != "AudioClip":
                continue
            g = row.get("assetGuid") or ""
            n = row.get("name") or ""
            if not g or not n:
                continue
            old = out.get(g)
            if old is None:
                out[g] = {"name": n, "type": row.get("type"), "bundle": row.get("bundle"),
                          "source": source}
            elif old["name"] != n:
                print("[WARN] GUID %s 在 `%s` 里对上两个不同 AudioClip（`%s` / `%s`）—— 保留第一个"
                      % (g, source, old["name"], n))
    return out, None


def _load_guid_map():
    """束枚举表优先、旧表兜底。返回 (map, 说明 list)。"""
    primary, err1 = _read_guid_map(GUID_MAP_BUNDLES, "bundle-container")
    if err1:
        raise SystemExit("[FATAL] " + err1 + " —— customSound 没法解资产名；"
                         "先跑 `python 工具/gen_guid_map_from_bundles.py`（⛔ 别猜）")
    fallback, err2 = _read_guid_map(GUID_MAP_OLD, "old-map")
    notes = ["primary=%s（%d 条 AudioClip）" % (GUID_MAP_BUNDLES, len(primary))]
    if err2:
        notes.append("fallback 不可用（%s）" % err2)
    else:
        notes.append("fallback=%s（%d 条 AudioClip，只补 primary 没有的）" % (GUID_MAP_OLD, len(fallback)))
    merged = dict(fallback)
    merged.update(primary)          # primary 压过 fallback（它是当前 build 的束枚举）
    return merged, notes


def build():
    raw_doc = _read_json(CARDS_RAW)
    raw_cards = raw_doc["cards"]
    i2 = _read_json(LOC_ZH_EN)["terms"]
    engine_ids = {c["id"] for c in _read_json(CARDS_ENGINE)["cards"]}
    guid_map, guid_notes = _load_guid_map()

    # customSound 的对账口径（运行时件与全量件共用同一套）
    sound_guids = set()
    sound_unresolved = set()
    n_sound_entries = 0

    def sound_asset(sg):
        """GUID → AudioClip 资产名；解不出 ⇒ 空串（并记进 `sound_unresolved`）。"""
        nonlocal n_sound_entries
        if not sg:
            return ""
        n_sound_entries += 1
        sound_guids.add(sg)
        rec = guid_map.get(sg)
        if rec is None:
            sound_unresolved.add(sg)
            return ""
        return rec["name"]

    selected = []
    for c in raw_cards:
        fr = c.get("full_raw") or {}
        if (_ne(fr.get("customIntroChatData"))
                or _ne(fr.get("customIntroByArmyChatData"))
                or _ne(fr.get("chatMessagesReferenceId"))
                or _ne(fr.get("customIntroReferenceId"))):
            selected.append(c)
    selected.sort(key=lambda c: c["uniqueId"])

    cards = []
    missing_keys = []
    seen_missing = set()
    n_prec = n_army = 0
    n_prec_uniq = n_army_uniq = 0

    for c in selected:
        fr = c["full_raw"]
        uid = c["uniqueId"]
        name = c.get("cardName") or c.get("m_Name") or ""
        army = c.get("cardArmy")
        if army not in CARD_ARMY:
            raise SystemExit("[FATAL] 未知 cardArmy=%r（卡 %s）—— 别猜，先补判据" % (army, uid))
        chat_ref = fr.get("chatMessagesReferenceId") or ""
        intro_ref = fr.get("customIntroReferenceId") or ""
        # RawCardScript.GetChatId(_, true) = 0x168 ?: 0x170 ?: 0x38
        chat_id = chat_ref or intro_ref or uid

        intro_by_opponent = {}
        dup_opponents = []
        for e in fr.get("customIntroChatData") or []:
            n_prec += 1
            opp = e.get("opponentWarlord") or ""
            if not opp:
                raise SystemExit("[FATAL] 卡 %s 有空的 opponentWarlord" % uid)
            if opp in intro_by_opponent:
                # 原版 List 逐条比、**第一条命中就返回** ⇒ 保留第一条；
                # 实测只有 GOF27→ASH2 一处重复，且两条内容逐字段相同。
                dup_opponents.append(opp)
                continue
            n_prec_uniq += 1
            key = CHAT_PREFIX + chat_id + "-Chat" + str(INTRO_IDX) + "-" + opp
            sg = _sound_guid(e)
            intro_by_opponent[opp] = {
                "key": key,
                "talksFirst": int(e.get("talksFirst") or 0),
                "hasText": key in i2,
                "soundGuid": sg,
                "soundAsset": sound_asset(sg),
            }
            if key not in i2 and key not in seen_missing:
                seen_missing.add(key)
                missing_keys.append({
                    "card": uid,
                    "name": name,
                    "key": key,
                    "inRuntime": uid in engine_ids,
                })

        intro_by_army = {}
        dup_armies = []
        for e in fr.get("customIntroByArmyChatData") or []:
            n_army += 1
            a = e.get("army")
            if a not in CARD_ARMY:
                raise SystemExit("[FATAL] 卡 %s 有未知 army=%r" % (uid, a))
            aname = CARD_ARMY[a]
            if aname in intro_by_army:
                dup_armies.append(aname)
                continue
            n_army_uniq += 1
            key = CHAT_PREFIX + chat_id + "-Chat" + str(INTRO_IDX) + "-" + aname
            sg = _sound_guid(e)
            intro_by_army[aname] = {
                "key": key,
                "hasText": key in i2,
                "soundGuid": sg,
                "soundAsset": sound_asset(sg),
            }
            if key not in i2 and key not in seen_missing:
                seen_missing.add(key)
                missing_keys.append({
                    "card": uid,
                    "name": name,
                    "key": key,
                    "inRuntime": uid in engine_ids,
                })

        # talksFirst：**逐对手**。只有全部精确档条目一致时才给标量；
        # 否则 null（消费方看 introByOpponent[*].talksFirst）。
        tf_vals = sorted({v["talksFirst"] for v in intro_by_opponent.values()})
        talks_first = tf_vals[0] if len(tf_vals) == 1 else None

        cards.append({
            "id": uid,
            "name": name,
            "army": army,
            "armyName": CARD_ARMY[army],
            "inRuntime": uid in engine_ids,
            "chatRef": chat_ref,
            "introRef": intro_ref,
            "chatId": chat_id,
            "talksFirst": talks_first,
            "introByOpponent": intro_by_opponent,
            "introByArmy": intro_by_army,
            "duplicateOpponentEntries": dup_opponents,
            "duplicateArmyEntries": dup_armies,
        })

    # ---------- 自检（出声，不静默） ----------
    rt_cards = [c for c in cards if c["inRuntime"]]
    for c in rt_cards:
        if c["chatId"] != c["id"]:
            # 目前 0 例；真出现就在运行时件里带覆盖表（消费方必须按它算 foe chatId）。
            print("[WARN] 运行时卡 %s 的 chatId=%s != id —— 已写进 chatIdOverrides" % (c["id"], c["chatId"]))
    chat_id_overrides = {c["id"]: c["chatId"] for c in rt_cards if c["chatId"] != c["id"]}

    rt_missing = sorted({m["key"] for m in missing_keys if m["inRuntime"]})
    all_missing = sorted({m["key"] for m in missing_keys})
    rt_keys = 0
    rt_unique = set()
    rt_with_text = 0
    for c in rt_cards:
        for v in c["introByOpponent"].values():
            rt_keys += 1
            rt_unique.add(v["key"])
            rt_with_text += 1 if v["hasText"] else 0
        for v in c["introByArmy"].values():
            rt_keys += 1
            rt_unique.add(v["key"])
            rt_with_text += 1 if v["hasText"] else 0
    all_keys = 0
    all_unique = set()
    all_with_text = 0
    for c in cards:
        for v in c["introByOpponent"].values():
            all_keys += 1
            all_unique.add(v["key"])
            all_with_text += 1 if v["hasText"] else 0
        for v in c["introByArmy"].values():
            all_keys += 1
            all_unique.add(v["key"])
            all_with_text += 1 if v["hasText"] else 0

    rt_sound_entries = sum(
        1 for c in rt_cards
        for v in list(c["introByOpponent"].values()) + list(c["introByArmy"].values())
        if v.get("soundGuid"))
    rt_sound_unresolved = sorted({
        v["soundGuid"] for c in rt_cards
        for v in list(c["introByOpponent"].values()) + list(c["introByArmy"].values())
        if v.get("soundGuid") and not v.get("soundAsset")})

    counts = {
        "soCards": len(cards),
        "runtimeCards": len(rt_cards),
        "nonRuntimeCards": len(cards) - len(rt_cards),
        "preciseEntriesRaw": n_prec,
        "preciseEntries": n_prec_uniq,
        "byArmyEntries": n_army_uniq,
        # 条目数（同一张卡的重复 SO 会拼出相同的键 —— 例：GUID 副本 chatRef 指回短 id）
        "keysTotal": all_keys,
        "keysTotalRuntime": rt_keys,
        # 去重后的键数（这才是「官方表里查了多少个不同的键」）
        "uniqueKeys": len(all_unique),
        "uniqueKeysRuntime": len(rt_unique),
        "keysWithText": all_with_text,
        "keysWithTextRuntime": rt_with_text,
        "keysMissing": len(all_missing),
        "keysMissingRuntime": len(rt_missing),
        # 🆕 2026-10-20（`T18`）：`customSound`（原版 `ChatManager.GetChatSoundAsset` 的定制音）
        "soundEntries": n_sound_entries,
        "soundEntriesRuntime": rt_sound_entries,
        "soundGuids": len(sound_guids),
        "soundGuidsResolved": len(sound_guids) - len(sound_unresolved),
        "soundGuidsUnresolved": len(sound_unresolved),
    }

    algorithm = {
        "chatId": "chatMessagesReferenceId(+0x168) ?: customIntroReferenceId(+0x170) ?: uniqueId(+0x38)"
                  "  —— RawCardScript__GetChatId.c",
        "foeChatId": "对手卡同一条链（RawCardScript__GetCustomIntro 里 GetChatId(foe, true)，param2=1）",
        "precise": "Chat_Messages/<meChatId>-Chat0-<foeChatId>"
                   "  —— RawCardScript__GetLocalizedCustomIntroChatMsg.c",
        "byArmy": "Chat_Messages/<meChatId>-Chat0-<CardArmy 枚举名>"
                  "  —— RawCardScript__GetLocalizedCustomIntroChatMsgByArmy.c（System.Enum.ToString）",
        "fallback": "Chat_Messages/<chatMessagesReferenceId ?: uniqueId>-Chat0"
                    "  —— RawCardScript__GetLocalizedChatMsg.c（⚠️ 这一档不看 customIntroReferenceId）",
        "idx": "ChatMessage.Intro = 0；本表只出 Intro。官方表 -Chat0-* 之外的带第三段的键只有 2 条杂键"
               "（ASH2-Chat8-OUT / AM66-Chat10-old），与开场白无关。"
               "同一战将（MirrorMatch）没有 -Chat1-* 定制键：SO 里没有任何卡片把自己列进"
               "customIntroChatData（现读 0 条），官方表里也没有一条 -Chat1-<第三段>。",
        "talksFirst": "ChatManager__IsPlayerFirstIntro.c：只查精确档；命中返回 entry.talksFirst；"
                      "未命中（含只命中按阵营档）⇒ 1（玩家先说）。逐对手不同 ⇒ talksFirst=null +"
                      " introByOpponent[*].talksFirst。",
        "preciseGate": "精确档的闸门是 SO 的 customIntroChatData 列表（GetCustomIntro 逐条比），"
                       "不是官方表里有哪些键；表里多出来的键 = 死键。",
        "introRefRedirect": "customIntroReferenceId(+0x170) 非空时，GetCustomIntro 会先"
                            " CollectionManager.GetRawCardFromId(那个 id) 把 me 重定向到该卡，"
                            "数据列表取那张卡的（本表按 SO 原样导出各自的列表；运行时 49 张"
                            "chatRef/introRef 全空 ⇒ 不含重定向差异）。",
        "vs": "-vs* 18 条：stringliteral.json 无 '-vs' 字面量、反编译无方法拼它 ⇒ 旧版本遗留，本表不建。",
        "customSound": "ChatManager__GetChatSoundAsset.c（idx=0）：① 精确档条目命中且 "
                       "entry.customSound(+0x18) 非空 ⇒ `AssetReferenceTyped<AudioClip>.Load` 播它；"
                       "② 否则按阵营档同一条；③ 两档都没有/条目没带 customSound ⇒ 回落 "
                       "RawCardScript.GetChatSound(idx)（`introChatSound` → `defaultChatSound`）。"
                       "⚠️ 运行时 49 张卡的 197 条条目**全部**带 customSound（现读）。"
                       "字段 `soundGuid` = `AssetReference.m_AssetGUID`，`soundAsset` = 束里 AudioClip 的资产名"
                       "（解出后我们按 `Resources/Art/audio/vo/<soundAsset>` 加载；解不出 ⇒ 空串 + 进 "
                       "`unresolvedSoundGuids`，⛔ 不猜成默认音）。",
    }

    full_doc = {
        "schema": "card_chat_intro/1",
        "generator": "工具/gen_card_chat_intro.py",
        "source": {
            "cardSO": "资料/普查产出_1018/allcards_1350/cards_raw.json"
                      "（当前 build 2026-10-18 · 1,351 张 RawCardScript；bundle=%s）" % raw_doc.get("bundle", ""),
            "localization": "数据/游戏数据/官方本地化_en+zh-CN.json（当前 build en/zh-CN 两列）",
            "cardsEngine": "MyGame/Assets/RuleEngine/Resources/cards_engine.json（只用来判 inRuntime）",
            "soundGuidMap": " · ".join(guid_notes),
            "decompiled": "d:/2/tools/decomp_full/：ChatManager__GetChatMessage.c · ChatManager__GetCustomIntro.c"
                          " · ChatManager__GetCustomIntroByArmy.c · ChatManager__IsPlayerFirstIntro.c"
                          " · RawCardScript__GetChatId.c · RawCardScript__GetLocalizedCustomIntroChatMsg.c"
                          " · RawCardScript__GetLocalizedCustomIntroChatMsgByArmy.c"
                          " · RawCardScript__GetLocalizedChatMsg.c",
        },
        "note": "本条导【文本键 + talksFirst + customSound】。customSound 的 GUID 在 `soundGuid`、"
                "解出的 AudioClip 资产名在 `soundAsset`（解不出 ⇒ 空串并在 `unresolvedSoundGuids` 里，"
                "⛔ 不猜成默认音）。"
                "talksFirst=null 表示精确档逐对手不同（看 introByOpponent[*].talksFirst），"
                "或该卡没有精确档条目（此时原版 IsPlayerFirstIntro 返回 1）。"
                "hasText 是「这个键在官方 en+zh-CN 表里存不存在」（en 与 zh-CN 任一非空才算有；"
                "13lang 运行时表是本表的超集：10,854/10,854 全在）。",
        "algorithm": algorithm,
        "counts": counts,
        "missingKeys": all_missing,
        "unresolvedSoundGuids": sorted(sound_unresolved),
        "cards": cards,
    }

    rt_doc = {
        "schema": "card_chat_intro/1",
        "note": "原版定制开场白两档（精确档 / 按阵营档）+ talksFirst + customSound。"
                "键拼法见全量件 `数据/游戏数据/card_chat_intro.json` 的 algorithm 节。"
                "只收 cards_engine.json 里有的卡（inRuntime=true）；副本/教程 SO 见全量件。"
                "talksFirst=null ⇒ 逐对手不同（看 introByOpponent[*].talksFirst）或无精确档（默认 1）。"
                "hasText=false 的键在官方表里没有原文（⛔ 不补假键；Loc.T 会回显键名）。"
                "soundGuid = SO 的 `customSound.m_AssetGUID`；soundAsset = 解出的 AudioClip 资产名"
                "（加载路径 = `Resources/Art/audio/vo/<soundAsset>`；空串 = 这条没有定制音）。",
        "chatIdOverrides": chat_id_overrides,
        "counts": {
            "cards": len(rt_cards),
            "preciseEntries": sum(len(c["introByOpponent"]) for c in rt_cards),
            "byArmyEntries": sum(len(c["introByArmy"]) for c in rt_cards),
            "uniqueKeys": len(rt_unique),
            "keysWithText": rt_with_text,
            "keysMissing": len(rt_missing),
            "soundEntries": rt_sound_entries,
            "soundGuids": len({v.get("soundGuid")
                               for c in rt_cards
                               for v in list(c["introByOpponent"].values()) + list(c["introByArmy"].values())
                               if v.get("soundGuid")}),
            "soundGuidsUnresolved": len(rt_sound_unresolved),
        },
        "cards": [],
    }
    for c in rt_cards:
        rt_doc["cards"].append({
            "id": c["id"],
            "name": c["name"],
            "army": c["army"],
            "armyName": c["armyName"],
            "chatRef": c["chatRef"],
            "introRef": c["introRef"],
            "chatId": c["chatId"],
            "talksFirst": c["talksFirst"],
            "introByOpponent": {
                k: {"key": v["key"], "talksFirst": v["talksFirst"], "hasText": v["hasText"],
                    "soundGuid": v["soundGuid"], "soundAsset": v["soundAsset"]}
                for k, v in c["introByOpponent"].items()
            },
            "introByArmy": {
                k: {"key": v["key"], "hasText": v["hasText"],
                    "soundGuid": v["soundGuid"], "soundAsset": v["soundAsset"]}
                for k, v in c["introByArmy"].items()
            },
        })

    return full_doc, rt_doc, counts, rt_missing


def canon(doc):
    return json.dumps(doc, ensure_ascii=False, indent=1)


def main():
    full_doc, rt_doc, counts, rt_missing = build()
    full_text = canon(full_doc)
    rt_text = canon(rt_doc)

    check = "--check" in sys.argv
    if check:
        ok = True
        for p, t in ((OUT_FULL, full_text), (OUT_RT, rt_text)):
            old = io.open(p, encoding="utf-8").read() if os.path.exists(p) else ""
            same = old == t
            ok = ok and same
            print("[--check] %s 与盘上那份%s" % (p, "一致 ✅" if same else "**不同** ❌"))
        print("=== --check " + ("全过 ✅" if ok else "有差异 ✗"))
        return 0 if ok else 1

    # ⚠️ 先算进变量、确认没错再写（`io.open(p,'wb').write(<表达式>)` 会先截断再求值）
    for p, t in ((OUT_FULL, full_text), (OUT_RT, rt_text)):
        io.open(p, "w", encoding="utf-8", newline="\n").write(t)
        print("[写盘] %s  (%d 字节)" % (p, len(t.encode("utf-8"))))

    # Resources/ 下的资产要 .meta（照 `gen_lore.py` 的先例；**只在缺失时建一次**，之后重跑不改它）
    meta = OUT_RT + ".meta"
    if not os.path.isfile(meta):
        guid = os.urandom(16).hex()
        with io.open(meta, "w", encoding="utf-8", newline="\n") as f:
            f.write("fileFormatVersion: 2\n")
            f.write("guid: %s\n" % guid)
            f.write("TextScriptImporter:\n")
            f.write("  externalObjects: {}\n")
            f.write("  userData: \n")
            f.write("  assetBundleName: \n")
            f.write("  assetBundleVariant: \n")
        print("[写盘] %s  (guid %s)" % (meta, guid))

    print("[统计] SO 卡 %d（运行时 %d / 副本·教程 %d）｜精确档 %d 条（原 %d，去重后）｜按阵营档 %d 条"
          % (counts["soCards"], counts["runtimeCards"], counts["nonRuntimeCards"],
             counts["preciseEntries"], counts["preciseEntriesRaw"], counts["byArmyEntries"]))
    print("[统计] 键条目 %d（去重后 %d 个不同键；运行时件 %d 条 / %d 个）"
          % (counts["keysTotal"], counts["uniqueKeys"],
             counts["keysTotalRuntime"], counts["uniqueKeysRuntime"]))
    print("[统计] 官方表命中 %d 条 / 缺 %d 个（运行时命中 %d 条 / 缺 %d 个）"
          % (counts["keysWithText"], counts["keysMissing"],
             counts["keysWithTextRuntime"], counts["keysMissingRuntime"]))
    print("[统计] customSound 条目 %d（运行时 %d）｜GUID %d 个（解出 %d / 解不出 %d）"
          % (counts["soundEntries"], counts["soundEntriesRuntime"], counts["soundGuids"],
             counts["soundGuidsResolved"], counts["soundGuidsUnresolved"]))
    if full_doc.get("unresolvedSoundGuids"):
        print("[customSound GUID 解不出]（⛔ 不猜成默认音 —— 运行时件对应条目 soundAsset 为空串）")
        for g in full_doc["unresolvedSoundGuids"]:
            print("   " + g)
    if rt_missing:
        print("[运行时缺键]（⛔ 不补假键）")
        for k in rt_missing:
            print("   " + k)
    return 0


if __name__ == "__main__":
    sys.exit(main())
