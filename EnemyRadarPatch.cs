using HarmonyLib;
using UnityEngine;

namespace EnemyRadar;

[HarmonyPatch(typeof(Map))]
static class EnemyRadarPatch
{
    private static Sprite? _enemySprite;

    private static Sprite GetEnemySprite()
    {
        if (_enemySprite != null) return _enemySprite;

        int size = 4;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float radius = size / 2f - 0.5f;

        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
            }

        tex.Apply();
        _enemySprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 80f);
        return _enemySprite;
    }

    [HarmonyPostfix, HarmonyPatch(nameof(Map.ActiveSet))]
    private static void ActiveSet_Postfix(Map __instance, bool active)
    {
        if (!active) return;
        if (EnemyDirector.instance == null) return;
        if (EnemyDirector.instance.enemiesSpawned == null) return;

        foreach (EnemyParent enemyParent in EnemyDirector.instance.enemiesSpawned)
        {
            if (enemyParent == null) continue;
            if (!enemyParent.Spawned) continue;

            // Remove existing MapCustom to avoid duplicates
            MapCustom existing = enemyParent.GetComponent<MapCustom>();
            if (existing != null) Object.Destroy(existing);

            // Add fresh MapCustom
            MapCustom mapCustom = enemyParent.gameObject.AddComponent<MapCustom>();
            mapCustom.color = Color.red;
            mapCustom.sprite = GetEnemySprite();

            __instance.AddCustom(mapCustom, GetEnemySprite(), Color.red);

            // Wait for mapCustomEntity then set Parent
            EnemyRadar.Instance.StartCoroutine(
                EnemyRadar.SetEnemyDotParent(mapCustom, enemyParent.transform)
            );

            EnemyRadar.Logger.LogInfo($"Drew red dot for {enemyParent.gameObject.name}");
        }
    }
}