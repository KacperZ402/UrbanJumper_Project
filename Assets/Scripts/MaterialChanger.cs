using UnityEngine;
using System.Collections.Generic;

public class MaterialReplacer : MonoBehaviour
{
    [Header("Materia³y do podmiany")]
    [SerializeField] private List<Material> oldMaterials = new List<Material>();

    [Header("Nowe materia³y (kolejno odpowiadaj¹ce powy¿szym)")]
    [SerializeField] private List<Material> newMaterials = new List<Material>();

    void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend == null) return;

        Material[] mats = rend.materials;

        for (int i = 0; i < mats.Length; i++)
        {
            int index = oldMaterials.IndexOf(mats[i]); // sprawdzamy czy materia³ jest na liœcie
            if (index != -1 && index < newMaterials.Count && newMaterials[index] != null)
            {
                mats[i] = newMaterials[index]; // podmieniamy
            }
        }

        rend.materials = mats; // aktualizacja
    }
}