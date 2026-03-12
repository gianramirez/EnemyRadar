using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Collections;

namespace EnemyRadar;

[BepInPlugin("Gian.EnemyRadar", "EnemyRadar", "1.0")]
public class EnemyRadar : BaseUnityPlugin
{
    internal static EnemyRadar Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }

    private void Awake()
    {
        Instance = this;

        // Prevent the plugin from being deleted
        this.gameObject.transform.parent = null;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;

        Patch();

        Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
    }

    internal void Patch()
    {
        Harmony ??= new Harmony(Info.Metadata.GUID);
        Harmony.PatchAll();
    }

    internal void Unpatch()
    {
        Harmony?.UnpatchSelf();
    }

    private void Update()
    {
        if (Map.Instance == null || !Map.Instance.Active) return;
        if (EnemyDirector.instance == null) return;

        foreach (EnemyParent enemyParent in EnemyDirector.instance.enemiesSpawned)
        {
            if (enemyParent == null) continue;
            if (!enemyParent.Spawned) continue;

            MapCustom mapCustom = enemyParent.GetComponent<MapCustom>();
            if (mapCustom == null) continue;
            if (mapCustom.mapCustomEntity == null) continue;

            Map.Instance.CustomPositionSet(
                mapCustom.mapCustomEntity.transform,
                enemyParent.transform
            );
        }
    }

    internal static IEnumerator SetEnemyDotParent(MapCustom mapCustom, Transform enemyTransform)
    {
        float timeout = 3f;
        while (mapCustom.mapCustomEntity == null && timeout > 0f)
        {
            timeout -= 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (mapCustom.mapCustomEntity != null)
        {
            mapCustom.mapCustomEntity.Parent = enemyTransform;
            Logger.LogInfo($"Parent set to {enemyTransform.name}");
        }
        else
        {
            Logger.LogWarning($"Timed out waiting for mapCustomEntity on {enemyTransform.name}");
        }
    }
}