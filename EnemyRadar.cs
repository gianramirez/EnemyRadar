using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Collections;
using TMPro;

namespace EnemyRadar;

[BepInPlugin("Gian.EnemyRadar", "EnemyRadar", "1.0")]
public class EnemyRadar : BaseUnityPlugin
{
    internal static EnemyRadar Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }

    private static GameObject? _countLabel;

    private void Awake()
    {
        Instance = this;
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

            Transform trackTransform = enemyParent.Enemy != null
                ? enemyParent.Enemy.transform
                : enemyParent.transform;

            Map.Instance.CustomPositionSet(
                mapCustom.mapCustomEntity.transform,
                trackTransform
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

    internal static void UpdateEnemyCountLabel(int count)
    {
        if (_countLabel == null)
        {
            GameObject canvasGO = new GameObject("EnemyRadar_Canvas");
            Object.DontDestroyOnLoad(canvasGO);
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            _countLabel = new GameObject("EnemyRadar_CountLabel");
            _countLabel.transform.SetParent(canvasGO.transform, false);

            TextMeshProUGUI tmp = _countLabel.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 24;
            tmp.color = Color.red;
            tmp.fontStyle = FontStyles.Bold;

            RectTransform rt = _countLabel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(20f, -20f);
            rt.sizeDelta = new Vector2(200f, 50f);
        }

        _countLabel.GetComponent<TextMeshProUGUI>().text = $"ENEMIES: {count}";
        _countLabel.transform.parent.gameObject.SetActive(
            Map.Instance != null && Map.Instance.Active
        );
        Logger.LogInfo($"Enemy count label updated: {count}");
    }
}