// OffensiveCards.cs — **进攻卡（= 环境效果卡）的数据入口**（§25）
//
// 数据来源：`Resources/OffensiveCards.json` —— 由 `工具/gen_offensive_cards_flat.py` 从
// **`数据/游戏数据/offensive_cards.json`**（子代理普查出来的正表）**只做形状转换**得来
// （`JsonUtility` 不支持字典，所以要摊成数组）。**判据与逐条证据全在源表里，这里一个判断都不加。**
//
// 每阵营的构成（原版 `EnviromentalEffectCardsSO` 一条 = 一个阵营）：
//   · **3 张进攻卡**（各带 `envSO` / `prefab` / `face` / `forgeLevel`）
//   · **1 张「不使用进攻卡」**（`emptyOffensiveCard`；卡名恒为 `Normal Conditions` 那一族，
//     `envSO` 为空 = 不换任何环境，只按 `blendTime` 补间环境光/雾）
//   · 一条 `defaultEnviromentalEffectVFX`（默认环境 SO）
// ⚠️ **卡名是「候选」**（`nameFrom` 标明来路）：卡引用在**远端 CCD**，本地解不出真名
//   （`cardName` 0/39）⇒ 表里给的是按「SO 名 → 贴图名」反推的候选，**别当卡面名用**。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    public static class OffensiveCards
    {
        [Serializable]
        public class Card
        {
            public int idx;             // 槽号 0/1/2（⚠️ **卡槽顺序 ≠ PnP 编号顺序**）
            public string name;         // 卡名**候选**（见文件头）
            public string nameFrom;     // 那个候选是怎么推出来的（如实标）
            public string envSO;        // 环境 SO 名（`ApplyEnvironment` 要的那一条）
            public string prefab;       // 环境 prefab 名（43 件那批）
            public string face;         // 卡面插画在原版贴图表里的名字（我们用同名的 PNG，见 `CardArt.OffensiveFace`）
            public int forgeLevel;      // 原版按锻造等级解锁（**我们全解锁**，只作记录）
        }

        [Serializable]
        public class Army
        {
            public string army;
            public int armyId;
            public string defaultEnvSO;
            public string emptyName;    // 「不使用进攻卡」那张的卡名（`Normal Conditions` 一族）
            public string emptyFace;
            public Card[] cards;
        }

        [Serializable] class File { public Army[] armies; }

        static Army[] _armies;
        static bool _tried;
        static readonly Dictionary<string, Army> _byName = new Dictionary<string, Army>();

        public static Army[] All { get { Load(); return _armies; } }
        public static bool Available { get { Load(); return _armies != null && _armies.Length > 0; } }

        /// <summary>按**我们的阵营名**取（= `CardDef.Faction`；大小写不敏感）。取不到返回 null。</summary>
        public static Army For(string faction)
        {
            Load();
            if (string.IsNullOrEmpty(faction)) return null;
            Army a;
            return _byName.TryGetValue(faction.Trim().ToLowerInvariant(), out a) ? a : null;
        }

        /// <summary>这一阵营的**可选卡列表** —— 原版 `GetEnvEffectCards` 的形状：
        /// **`[空卡, 进攻1, 进攻2, 进攻3]`**（空卡固定在**下标 0**；出处
        /// `BattleManager__GetEnvEffectCards.c:50-74` —— `shouldUseOffensive` 为真时**先** `Add(空卡)`
        /// 再 `AddRange(GetEnvCardList(...))`）。
        /// ⚠️ 原版 `GetEnvCardList(justOwnedCards:1)` 还会把 `forgeLevel > 当前锻造等级` 的槽**直接丢掉**
        /// （不是占位）—— **我们全解锁** ⇒ 三张都在（那是我们的落地，代码/文档都如实标）。</summary>
        public static List<Card> Choices(string faction)
        {
            var outp = new List<Card>();
            var a = For(faction);
            if (a == null) return outp;
            outp.Add(new Card { idx = -1, name = a.emptyName, envSO = null, prefab = null, face = a.emptyFace,
                                nameFrom = "「不使用进攻卡」那张（原版 `emptyOffensiveCard`，没有 envSO）；"
                                         + "**原版把它放在下标 0**" });
            if (a.cards != null) foreach (var c in a.cards) outp.Add(c);
            return outp;
        }

        /// <summary>这张是不是「不使用进攻卡」那张（原版 `isEmptyOffensiveCard` 判的就是它）。</summary>
        public static bool IsEmpty(Card c) { return c != null && c.idx < 0; }

        static void Load()
        {
            if (_tried) return;
            _tried = true;
            var ta = Resources.Load<TextAsset>("OffensiveCards");
            if (ta == null)
            {
                Debug.LogWarning("[OffensiveCards] 读不到 `Resources/OffensiveCards.json` ⇒ 进攻卡这一条链不可用"
                               + "（跑 `工具/gen_offensive_cards_flat.py` 生成它）");
                return;
            }
            var f = JsonUtility.FromJson<File>(ta.text);
            if (f == null || f.armies == null || f.armies.Length == 0)
            {
                Debug.LogWarning("[OffensiveCards] `OffensiveCards.json` 解出来是空的 —— 不静默");
                return;
            }
            _armies = f.armies;
            foreach (var a in _armies)
                if (!string.IsNullOrEmpty(a.army)) _byName[a.army.Trim().ToLowerInvariant()] = a;
            Debug.Log($"[OffensiveCards] {_armies.Length} 个阵营 · "
                    + $"{Count()} 个卡槽（+ 每阵营 1 张「不使用进攻卡」）");
        }

        static int Count()
        {
            int n = 0;
            if (_armies != null) foreach (var a in _armies) n += a.cards != null ? a.cards.Length : 0;
            return n;
        }
    }
}
