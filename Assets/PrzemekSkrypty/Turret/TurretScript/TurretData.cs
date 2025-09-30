using UnityEngine;

[CreateAssetMenu(fileName = "New Turret", menuName = "Tower Defense/Turret")]
public class TurretData : ScriptableObject
{
    [Header("Podstawowe informacje")]
    [Tooltip("Nazwa wie¿yczki")]
    public string turretName;
    [Tooltip("Opis dzia³ania wie¿yczki")]
    public string description;

    // NOWOŒÆ: Ka¿dy poziom wie¿y ma swój w³asny prefab wizualny
    [Tooltip("Prefab wizualny TEGO poziomu wie¿y.")]
    public GameObject displayPrefab;

    [Header("Statystyki bojowe")]
    public float damage = 10f;
    public float fireRate = 1f;
    public float range = 5f;

    [Header("Koszt budowy")]
    [Tooltip("Koszt postawienia tej wie¿y")]
    public int cost = 50;

    [Tooltip("Koszt ulepszenia do TEGO poziomu (z poprzedniego).")]
    public int upgradeCost = 75;

    [Header("Ulepszenie")]
    [Tooltip("Dane nastêpnego poziomu wie¿y. Zostaw puste (None) dla maksymalnego poziomu.")]
    //public TurretData upgradeTo;

    public TurretData[] upgradePaths;
}

