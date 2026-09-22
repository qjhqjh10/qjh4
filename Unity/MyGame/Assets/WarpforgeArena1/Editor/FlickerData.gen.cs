// ─────────────────────────────────────────────────────────────────────────────
// 自动生成 —— **不要手改**！生成器：`工具/gen_flicker_cs.py`
//
// 原版 13 个战场里有 38 个对象挂着 `MaterialFlickerEffect`（材质闪烁脚本），我们从来没抽过。
// 算式与常量来自反编译（`MaterialFlickerEffect__Update.c` + `.rdata`），
// 并已用**原版实况**验过：alpha 在 0.20~2.03 之间动、均值 ≈1.0（见资料/战场13场_逐场对账_0920.md §一 ①-a）。
// ⚠️ 只有 `desync` 是「我们挑的」（原版用 UnityEngine.Random，改成按名字定死 ⇒ 可复现）。
// ─────────────────────────────────────────────────────────────────────────────

public static class FlickerData
{
    public struct Spec
    {
        public string arena;      // 场名（= arenas/<arena>/）
        public string go;         // 对象名
        public float amplitude;   // m_amplitude
        public float frequency;   // m_frequency
        public float fadeInTime;  // m_fadeInTime
        public float fadeOutTime; // m_fadeOutTime
        public bool  randomStart; // m_randomStart
        public float desync;      // ⚠️ 我们定的（原版 Random.Range(0,100)）
    }

    /// <summary>38 条，逐字来自原版场景（出处见生成器头）。</summary>
    public static readonly Spec[] Specs = new Spec[]
    {
        new Spec { arena = "battlearena1", go = "Barrels Light FX", amplitude = 0.8339999914169312f, frequency = 3.0f, fadeInTime = 0.75f, fadeOutTime = 0.5f, randomStart = true, desync = 29.32f },
        new Spec { arena = "battlearena1", go = "Generator 2 FX", amplitude = 0.8339999914169312f, frequency = 10.0f, fadeInTime = 0.5f, fadeOutTime = 0.5f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearena2", go = "DL_6_Torch_Right", amplitude = 0.25f, frequency = 5.0f, fadeInTime = 4.0f, fadeOutTime = 5.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearena2", go = "DL_2_Ground", amplitude = 0.4569999873638153f, frequency = 1.0f, fadeInTime = 4.0f, fadeOutTime = 4.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearena2", go = "DL_1_Trashpile", amplitude = 0.25f, frequency = 10.0f, fadeInTime = 4.0f, fadeOutTime = 4.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearena2", go = "DL_4_Tower_Left", amplitude = 0.25f, frequency = 4.0f, fadeInTime = 5.0f, fadeOutTime = 4.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearena2", go = "DL_5_Torch_Left", amplitude = 0.25f, frequency = 4.0f, fadeInTime = 5.0f, fadeOutTime = 4.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearena2", go = "DL_7_Tower_Right", amplitude = 0.25f, frequency = 5.0f, fadeInTime = 4.0f, fadeOutTime = 5.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearena3", go = "Props 1.001", amplitude = 0.15000000596046448f, frequency = 3.0f, fadeInTime = 2.0f, fadeOutTime = 2.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearena3", go = "Ground.001", amplitude = 0.24500000476837158f, frequency = 2.0f, fadeInTime = 2.0f, fadeOutTime = 3.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearena3", go = "Dynamic Lights 1", amplitude = 0.24500000476837158f, frequency = 2.0f, fadeInTime = 2.0f, fadeOutTime = 3.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearena3", go = "Ground.002", amplitude = 0.15000000596046448f, frequency = 3.0f, fadeInTime = 2.0f, fadeOutTime = 2.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenaaeldari", go = "Dynamic Lights 8", amplitude = 0.36000001430511475f, frequency = 2.0f, fadeInTime = 1.0f, fadeOutTime = 1.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenaaeldari", go = "Dynamic Lights 16", amplitude = 0.36000001430511475f, frequency = 2.0f, fadeInTime = 1.0f, fadeOutTime = 1.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenaaeldari", go = "Dynamic Lights 10", amplitude = 0.36000001430511475f, frequency = 2.0f, fadeInTime = 1.0f, fadeOutTime = 1.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenaaeldari", go = "Dynamic Lights 11", amplitude = 0.36000001430511475f, frequency = 2.0f, fadeInTime = 1.0f, fadeOutTime = 1.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenaaeldari", go = "Dynamic Lights 9", amplitude = 0.36000001430511475f, frequency = 2.0f, fadeInTime = 1.0f, fadeOutTime = 1.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenaaeldari", go = "Dynamic Lights 6", amplitude = 0.17499999701976776f, frequency = 5.0f, fadeInTime = 1.0f, fadeOutTime = 1.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenaaeldari", go = "Dynamic Lights 5", amplitude = 0.36000001430511475f, frequency = 2.0f, fadeInTime = 1.0f, fadeOutTime = 1.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenaastramilitarum", go = "Fake Light Glow (9)", amplitude = 0.4000000059604645f, frequency = 6.0f, fadeInTime = 5.0f, fadeOutTime = 5.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenaastramilitarum", go = "Fake Light Glow (5)", amplitude = 0.4000000059604645f, frequency = 1.5800000429153442f, fadeInTime = 5.0f, fadeOutTime = 5.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenaastramilitarum", go = "Fake Light Glow", amplitude = 0.4000000059604645f, frequency = 4.0f, fadeInTime = 5.0f, fadeOutTime = 5.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenaastramilitarum", go = "Fake Light Glow (8)", amplitude = 0.5f, frequency = 3.0f, fadeInTime = 4.0f, fadeOutTime = 5.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenaastramilitarum", go = "Fake Light Glow (1)", amplitude = 0.30000001192092896f, frequency = 2.0f, fadeInTime = 5.0f, fadeOutTime = 5.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenaastramilitarum", go = "Fake Light Glow (6)", amplitude = 0.4000000059604645f, frequency = 1.0f, fadeInTime = 7.0f, fadeOutTime = 5.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenaastramilitarum", go = "Fake Light Glow (4)", amplitude = 0.4000000059604645f, frequency = 2.0f, fadeInTime = 5.0f, fadeOutTime = 5.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenaastramilitarum", go = "Fake Light Glow (2)", amplitude = 0.5f, frequency = 3.0f, fadeInTime = 4.0f, fadeOutTime = 5.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenaastramilitarum", go = "Fake Light Glow (7)", amplitude = 0.4000000059604645f, frequency = 1.0f, fadeInTime = 7.0f, fadeOutTime = 5.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenaspacewolves", go = "Dynamic Lights 2", amplitude = 0.17499999701976776f, frequency = 2.5f, fadeInTime = 5.0f, fadeOutTime = 5.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenaspacewolves", go = "Dynamic Lights 1", amplitude = 0.17499999701976776f, frequency = 2.5f, fadeInTime = 5.0f, fadeOutTime = 5.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenaspacewolves", go = "Dynamic Lights 4", amplitude = 0.17499999701976776f, frequency = 2.5f, fadeInTime = 5.0f, fadeOutTime = 5.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenaspacewolves", go = "Dynamic Lights 6", amplitude = 0.17499999701976776f, frequency = 2.5f, fadeInTime = 5.0f, fadeOutTime = 5.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenaspacewolves", go = "Dynamic Lights 5", amplitude = 0.17499999701976776f, frequency = 2.5f, fadeInTime = 5.0f, fadeOutTime = 5.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenaspacewolves", go = "Dynamic Lights 7", amplitude = 0.17499999701976776f, frequency = 2.5f, fadeInTime = 5.0f, fadeOutTime = 5.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenaspacewolves", go = "Dynamic Lights 3", amplitude = 0.17499999701976776f, frequency = 2.5f, fadeInTime = 5.0f, fadeOutTime = 5.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenatauviorla", go = "Dynamic Lights 5", amplitude = 0.17499999701976776f, frequency = 2.5f, fadeInTime = 5.0f, fadeOutTime = 5.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenatauviorla", go = "Dynamic Lights 4", amplitude = 0.17499999701976776f, frequency = 2.5f, fadeInTime = 5.0f, fadeOutTime = 5.0f, randomStart = false, desync = 0.0f },
        new Spec { arena = "battlearenatauviorla", go = "Dynamic Lights 1", amplitude = 0.17499999701976776f, frequency = 2.5f, fadeInTime = 5.0f, fadeOutTime = 5.0f, randomStart = false, desync = 0.0f },
    };
}
