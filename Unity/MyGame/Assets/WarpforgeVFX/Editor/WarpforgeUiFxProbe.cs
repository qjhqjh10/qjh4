// WarpforgeUiFxProbe.cs — `A31`（特效接线）的验收探针：9 个「待接线」prefab 到底能不能播（2026-10-24）
//
// ============================ 它验什么、不验什么 ============================
// 验（机制层，**能在批处理里判绿红**）：
//   A 组 · **9/9 在效果库里**、`entry.prefab` 非空（名字错了 / 重导后没重建库，两种都会在这里红）。
//   B 组 · **原版 `CraftingPanel.craftVFXs[]` 那张表**（`MainMenu Warpforge.unity` 逐行实读）：
//         rarity 1..4 两列逐字相等；**rarity 5 = Button 复用 4 + Card 回落 1**（原值，不是笔误）。
//   C 组 · **「播了真的会实例化」**：8 个 `Wildcard_Use_*` 逐个走 `WarpforgeUiEffect.PlayUi`，
//         断言 实例非空 / 名字 == prefab 名 / 父节点 == 传进去的节点 / `ActiveCount` +1；
//         再断 **世界缩放 = prefab 自带根缩放 × `PixelScale`（1/108）**（⚠️ 8 件里有 3 件根缩放是
//         **1.2**，`Card 2/3/4` —— 原版 `SetParent` 保世界缩放的必然结果），并做一条**灭自证**：
//         同一件用 `scale = 1` 裸播一次 ⇒ 两者比值必须 ≈ **108**
//         （把 `PlayUi` 里那一跳删掉，这条立刻红 —— 它只验「乘了」是**证明不了**的）。
//   D 组 · **音效不静默**：`Wildcard_Use_Button 1..4` 播完之后 `SoundSlotCount == 1`、
//         `UnwiredSoundCues == 0`，并且 `WFSoundBank` 里那四条 cue 各能挑出**真 clip**。
//   E 组 · **`Swarm Card Merge VFX` 的收尾语义**：prefab 上 `Animation` 播的是
//         `Swarm Triat Merge`（`m_PlayAutomatically = 1`），clip 在 **t = 1.2 s** 带动画事件
//         `OnAnimationEndEvent`；`WarpforgeEffectPlayer` 上那个接收口**存在**，
//         且**直调它之后实例真的没了**（`ActiveCount` 回到基线）——
//         把接收口删掉，这一组红（原版 `SwarmVFXController__OnAnimationEndEvent.c` 的 ③）。
//
// **不验**（说清楚，⛔ 别把「绿」读成「接好了」）：
//   · **哪一拍播** —— 8 个 `Wildcard_Use_*` 的调用点在 `Shell/CardDetailPopup.cs`、
//     `Swarm Card Merge VFX` 的在 `Battle/**` / `Core/CardView.cs`，**都不在本批白名单**；
//     它们没接上之前，本探针绿只说明**机制通**。缺口与逐条补法 → `普查产出_1023e/A31_特效接线.md`。
//   · **画面像不像** —— 那要进 player 看（`资料/真Play待验清单.md`）。
//
// ============================ 怎么跑（主对话） ============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod WarpforgeUiFxProbe.Run -logFile -
//   判绿红：日志里 `UIFX === X 过 / Y 红 ===` 那一行（逐条按行首 `UIFX ✓` / `UIFX ✗` 数）。
//
// ⚠️ **不落盘**：实例全在临时根下建、结尾 `KillAll()` + `DestroyImmediate`；
//   本探针**不 SaveScene**、不改任何资产。
using System.Reflection;
using UnityEditor;
using UnityEngine;
using WarpforgeVFX;

public static class WarpforgeUiFxProbe
{
    const string P = "UIFX ";
    const string PrefabDir = "Assets/WarpforgeVFX/Prefabs/";

    /// <summary>`A31` 那 9 件（8 个万能卡 + 1 个虫群合并）。</summary>
    static readonly string[] All9 =
    {
        "Wildcard_Use_Card 1 Common",
        "Wildcard_Use_Card 2 Rare",
        "Wildcard_Use_Card 3 Epic",
        "Wildcard_Use_Card 4 Legendary",
        "Wildcard_Use_Button 1 Common",
        "Wildcard_Use_Button 2 Rare",
        "Wildcard_Use_Button 3 Epic",
        "Wildcard_Use_Button 4 Legendary",
        "Swarm Card Merge VFX",
    };

    static int _ok, _bad;

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(P + "✓ " + what); }
        else { _bad++; Debug.LogError(P + "✗ " + what); }
    }

    public static void Run()
    {
        _ok = _bad = 0;
        WarpforgeEffectLibrary lib = WarpforgeEffectLibrary.Instance;
        Check(lib != null, "效果库读得到（`Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset`）");
        if (lib == null) { Done(); return; }

        Transform root = null;
        try
        {
            // ---------------------------------------------------------------- A
            foreach (var name in All9)
            {
                WFEffectEntry e;
                bool found = lib.TryGet(name, out e);
                Check(found && e != null && e.prefab != null,
                      $"A · 效果库有 `{name}` 且 prefab 引用非空"
                      + (found && e != null && e.prefab == null ? "（引空 ⇒ 重导过 prefab 后没重建库）" : ""));
                if (!found || e == null || e.prefab == null) continue;

                // 「播了真的会动」的前提：prefab 自带粒子，且 `playOnAwake = 1`
                // （`WarpforgeEffectPlayer.Play` 只 Instantiate 一次，**没有**逐发射器 `Play()`）。
                var pss = e.prefab.GetComponentsInChildren<ParticleSystem>(true);
                bool allAwake = pss.Length > 0;
                for (int i = 0; i < pss.Length; i++)
                    if (pss[i] == null || !pss[i].playOnAwake) { allAwake = false; break; }
                Check(allAwake,
                      $"A · `{name}` 有粒子（{pss.Length} 个）且 `playOnAwake` 全开"
                      + (pss.Length == 0 ? "（没有粒子 ⇒ 实例化了也什么都看不见）" : ""));
            }

            // ---------------------------------------------------------------- B
            Check(WarpforgeUiEffect.WildcardCraftPrefab(1, true) == "Wildcard_Use_Button 1 Common"
                  && WarpforgeUiEffect.WildcardCraftPrefab(1, false) == "Wildcard_Use_Card 1 Common",
                  "B · rarity 1 = Button/Card `… 1 Common`（原版 `craftVFXs` 第 1 行）");
            Check(WarpforgeUiEffect.WildcardCraftPrefab(2, true) == "Wildcard_Use_Button 2 Rare"
                  && WarpforgeUiEffect.WildcardCraftPrefab(2, false) == "Wildcard_Use_Card 2 Rare",
                  "B · rarity 2 = `… 2 Rare`（第 2 行）");
            Check(WarpforgeUiEffect.WildcardCraftPrefab(3, true) == "Wildcard_Use_Button 3 Epic"
                  && WarpforgeUiEffect.WildcardCraftPrefab(3, false) == "Wildcard_Use_Card 3 Epic",
                  "B · rarity 3 = `… 3 Epic`（第 3 行）");
            Check(WarpforgeUiEffect.WildcardCraftPrefab(4, true) == "Wildcard_Use_Button 4 Legendary"
                  && WarpforgeUiEffect.WildcardCraftPrefab(4, false) == "Wildcard_Use_Card 4 Legendary",
                  "B · rarity 4 = `… 4 Legendary`（第 4 行）");
            // 🔴 原版第 5 行就是复用的（Button → 4、Card → 1）—— 断它 = 防「有人觉得像笔误顺手改掉」
            Check(WarpforgeUiEffect.WildcardCraftPrefab(5, true) == "Wildcard_Use_Button 4 Legendary"
                  && WarpforgeUiEffect.WildcardCraftPrefab(5, false) == "Wildcard_Use_Card 1 Common",
                  "B · rarity 5 = Button **复用 4** + Card **回落 1**（原版第 5 行的原值）");
            Check(WarpforgeUiEffect.WildcardCraftPrefab(0, true) == null
                  && WarpforgeUiEffect.WildcardCraftPrefab(6, false) == null,
                  "B · 越界稀有度 0/6 ⇒ 返回 null（不回落、不猜）");

            // ---------------------------------------------------------------- C
            // `PixelScale` 必须 = 1/108（判据 = `LayoutSpace.DesignHeight / DesignPxH`）
            float px = WarpforgeUiEffect.PixelScale;
            Check(Mathf.Abs(px * 108f - 1f) < 1e-5f,
                  $"C · `PixelScale` = 1/108（实测 {px.ToString("R")} ⇒ ×108 = {(px * 108f).ToString("R")}）");

            root = new GameObject("WarpforgeUiFxProbe·临时根").transform;
            root.position = Vector3.zero;

            string[] wildcard8 = new string[8];
            System.Array.Copy(All9, wildcard8, 8);

            foreach (var name in wildcard8)
            {
                int before = WarpforgeEffectPlayer.ActiveCount;
                var pl = WarpforgeUiEffect.PlayUi(name, root, new Vector3(1.5f, 2.5f, 0f));

                Check(pl != null, $"C · `{name}` 播出来了（PlayUi 返回非空）");
                if (pl == null) continue;

                Check(pl.gameObject.name == name,
                      $"C · `{name}` 的实例名 == prefab 名（实测 `{pl.gameObject.name}`）");
                Check(pl.transform.parent == root,
                      $"C · `{name}` 的父节点 == 传进去的节点（实测 `{(pl.transform.parent == null ? "<null>" : pl.transform.parent.name)}`）");
                Check(WarpforgeEffectPlayer.ActiveCount == before + 1,
                      $"C · `{name}` 播完 `ActiveCount` +1（前 {before} → 后 {WarpforgeEffectPlayer.ActiveCount}）");

                // 原版是 `Instantiate(prefab, …)` 再 `SetParent(…, worldPositionStays = true)`
                // ⇒ **世界缩放 = prefab 自带的根缩放**（`SetParent` 保世界缩放）。
                // 🔴 实测这 8 件里 `Wildcard_Use_Card 2 Rare/3 Epic/4 Legendary` 的根缩放是 **1.2**
                //    （其余是 1）—— ⛔ 别一律断 `1/108`，那三条会假红。
                float prefabScale = (pl.Entry != null && pl.Entry.prefab != null)
                                    ? pl.Entry.prefab.transform.localScale.x : 1f;
                float expect = prefabScale * px;
                float ws = pl.transform.lossyScale.x;
                Check(Mathf.Abs(ws - expect) < 1e-5f,
                      $"C · `{name}` 的世界缩放 = prefab 根缩放 {prefabScale} × 1/108"
                      + $"（实测 {ws.ToString("R")}，期望 {expect.ToString("R")}）");

                // 灭自证：同一件**用同一组入参**裸播（scale = 1）⇒ 两者比值必须 ≈ 108。
                // 把 `PlayUi` 里乘 `PixelScale` 那一跳删掉 ⇒ 两个数相等 ⇒ 本条红。
                var raw = WarpforgeEffectPlayer.Play(name, root, new Vector3(1.5f, 2.5f, 0f), 1f, -1f);
                if (raw != null)
                {
                    float rs = raw.transform.lossyScale.x;
                    Check(Mathf.Abs(rs / Mathf.Max(1e-9f, ws) - 108f) < 0.5f,
                          $"C · 灭自证：`{name}` 裸播 vs PlayUi 的世界缩放比 ≈ 108（实测 {rs.ToString("R")} / {ws.ToString("R")} = {(rs / Mathf.Max(1e-9f, ws)).ToString("F3")}）");
                    raw.Kill();
                }
                else Check(false, $"C · 灭自证：`{name}` 的裸播对照组没建出来");
            }

            // ---------------------------------------------------------------- D
            for (int r = 1; r <= 4; r++)
            {
                string cue = "Wildcard Use " + r;
                WFSoundCue c;
                bool hasCue = WFSoundBank.TryGetCue(cue, out c);
                Check(hasCue, $"D · 音效表有 cue `{cue}`（原版 `Wildcard_Use_Button {r}` 的 `sounds[0].sound`）");
                if (hasCue)
                    Check(WFSoundBank.PickClip(c, null) != null,
                          $"D · cue `{cue}` 能挑出一条**真能加载**的 clip");
            }

            WarpforgeEffectPlayer.ResetSoundDiagnostics();
            var btn = WarpforgeUiEffect.PlayUi("Wildcard_Use_Button 2 Rare", root, Vector3.zero);
            if (btn != null)
            {
                Check(btn.SoundSlotCount == 1,
                      $"D · `Wildcard_Use_Button 2 Rare` 建出 1 条音效槽（实测 {btn.SoundSlotCount}）");
                Check(WarpforgeEffectPlayer.UnwiredSoundCues == 0,
                      $"D · 播完 `UnwiredSoundCues == 0`（实测 {WarpforgeEffectPlayer.UnwiredSoundCues}"
                      + (WarpforgeEffectPlayer.UnwiredSoundCues > 0 ? $"；坏 cue：{WarpforgeEffectPlayer.BadCueList}" : "") + "）");
                btn.Kill();
            }
            else Check(false, "D · `Wildcard_Use_Button 2 Rare` 播不出来 ⇒ 音效那两条没验到");

            // ---------------------------------------------------------------- E
            WFEffectEntry swarm;
            if (lib.TryGet("Swarm Card Merge VFX", out swarm) && swarm.prefab != null)
            {
                var anim = swarm.prefab.GetComponent<Animation>();
                Check(anim != null, "E · `Swarm Card Merge VFX` 根上有 `Animation` 组件");
                if (anim != null)
                {
                    Check(anim.playAutomatically, "E · `Animation.m_PlayAutomatically = 1`（原版同值）");
                    Check(anim.clip != null && anim.clip.name == "Swarm Triat Merge",
                          $"E · 播的 clip 是 `Swarm Triat Merge`（实测 `{(anim.clip == null ? "<null>" : anim.clip.name)}`）");
                    if (anim.clip != null)
                    {
                        var evs = AnimationUtility.GetAnimationEvents(anim.clip);
                        bool hasEv = false;
                        float at = -1f;
                        for (int i = 0; i < evs.Length; i++)
                            if (evs[i] != null && evs[i].functionName == "OnAnimationEndEvent") { hasEv = true; at = evs[i].time; }
                        Check(hasEv, "E · clip 上带动画事件 `OnAnimationEndEvent`（原版靠它在动画结束那一拍销毁）");
                        if (hasEv) Check(Mathf.Abs(at - 1.2f) < 0.01f,
                                         $"E · 该事件在 t = 1.2 s（实测 {at.ToString("F3")}）");
                    }
                }
            }
            else Check(false, "E · 效果库里没有 `Swarm Card Merge VFX`");

            // 接收口：结构断言（删掉方法 ⇒ 动画事件又没有接收者了）
            var mi = typeof(WarpforgeEffectPlayer).GetMethod(
                "OnAnimationEndEvent", BindingFlags.Public | BindingFlags.Instance);
            Check(mi != null, "E · `WarpforgeEffectPlayer.OnAnimationEndEvent` 存在（legacy `Animation` 的事件接收口）");

            // 行为断言：直调接收口 ⇒ 实例**真的销毁**
            // ⚠️ 这里用**裸 `Play`（scale = 1）**，⛔ 不用 `PlayUi` —— `Swarm Card Merge VFX` 是**战场世界件**
            //    （原版挂在卡的 transform 上、量纲是卡单位），不是像素单位的 UI 件。
            int baseCount = WarpforgeEffectPlayer.ActiveCount;
            var sw = WarpforgeEffectPlayer.Play("Swarm Card Merge VFX", root, Vector3.zero, 1f, -1f);
            if (sw != null)
            {
                Check(WarpforgeEffectPlayer.ActiveCount == baseCount + 1,
                      $"E · `Swarm Card Merge VFX` 播出来了（ActiveCount {baseCount} → {WarpforgeEffectPlayer.ActiveCount}）");
                sw.OnAnimationEndEvent();
                Check(WarpforgeEffectPlayer.ActiveCount == baseCount,
                      $"E · 直调 `OnAnimationEndEvent()` ⇒ 实例已销毁、`ActiveCount` 回基线（实测 {WarpforgeEffectPlayer.ActiveCount}"
                      + $" / 基线 {baseCount}）；没销毁的话它会活到 `natural` 3.5 s，比原版的 1.2 s 多 2.3 s");
            }
            else Check(false, "E · `Swarm Card Merge VFX` 播不出来");

            // ---------------------------------------------------------------- 信息（不计绿红）
            WFSoundCue sm;
            if (!WFSoundBank.TryGetCue("Trait Swarm Tyranid", out sm))
                Debug.LogWarning(P + "·缺口（不计红）`Swarm Card Merge VFX` 原版 `SwarmVFXController.sound` = "
                               + "cue `Trait Swarm Tyranid`（`Trait Swarm Tyranid.asset`，clip 同名 wav），"
                               + "**我们音效表里没有它、`Art/audio/sfx/` 里也没有那个 wav** ⇒ 虫群合并今天没有 3D 音。"
                               + "补法见 `普查产出_1023e/A31_特效接线.md` §五。");
        }
        finally
        {
            WarpforgeEffectPlayer.KillAll();
            if (root != null) Object.DestroyImmediate(root.gameObject);
        }
        Done();
    }

    static void Done()
    {
        Debug.Log(P + $"=== {_ok} 过 / {_bad} 红 ===");
        if (Application.isBatchMode) EditorApplication.Exit(_bad == 0 ? 0 : 1);
    }
}
