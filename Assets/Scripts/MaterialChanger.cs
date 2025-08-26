using UnityEngine;
using System.Collections.Generic;

public class RandomMaterialReplacer : MonoBehaviour
{
    [Tooltip("Który slot materia³u ma byæ podmieniany (0 = pierwszy)")]
    [SerializeField] private int materialIndex = 0;

    [Tooltip("Lista materia³ów do losowego wyboru")]
    [SerializeField] private List<Material> replacementMaterials = new List<Material>();

    void Start()
    {
        ApplyRandomMaterial();
    }

    public void ApplyRandomMaterial()
    {
        if (replacementMaterials.Count == 0) return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in renderers)
        {
            Material[] mats = rend.materials; // tworzy kopiê materia³ów dla tego obiektu

            if (materialIndex >= 0 && materialIndex < mats.Length)
            {
                Material randomMat = replacementMaterials[Random.Range(0, replacementMaterials.Count)];
                mats[materialIndex] = randomMat;
                rend.materials = mats; // nadpisanie tablicy
            }
        }
    }
}