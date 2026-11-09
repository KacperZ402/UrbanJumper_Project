using System.Collections.Generic;
using UnityEngine;

// Kolejka startów spawnerów: wykonuje zgłoszenia w Update,
// maksymalnie maxActionsPerFrame na klatkę i nie dłużej niż maxTimePerFrameMs.
// Siedzi w scenie, więc ginie razem z nią – po restarcie nie zostaje żaden "zawieszony" stan.
[DefaultExecutionOrder(-100)] // Awake przed poolem i spawnerami
public class SpawnWorkQueue : MonoBehaviour
{
    private struct WorkItem
    {
        public MonoBehaviour owner;
        public System.Action action;
    }

    [SerializeField] private int maxActionsPerFrame = 8;
    [SerializeField] private float maxTimePerFrameMs = 2f;

    [SerializeField] private int maxSpawnsPerFrame = 5;

    private int budgetFrame = -1;
    private int spawnsThisFrame;

    private static SpawnWorkQueue instance;
    private readonly Queue<WorkItem> queue = new Queue<WorkItem>();

    private void Awake()
    {
        instance = this; // kolejka z nowej sceny przejmuje pałeczkę
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // To samo API co wcześniej – spawnery nie wymagają zmian
    public static void Enqueue(MonoBehaviour owner, System.Action action)
    {
        if (owner == null || action == null) return;

        // brak kolejki w scenie – tworzymy ją sami (zginie razem ze sceną)
        if (instance == null)
            new GameObject("SpawnWorkQueue").AddComponent<SpawnWorkQueue>();

        instance.queue.Enqueue(new WorkItem { owner = owner, action = action });
    }
    // Globalny limit spawnów propów na klatkę – wspólny dla wszystkich spawnerów
    public static bool TryConsumeSpawn()
    {
        if (instance == null) return true;

        int frame = Time.frameCount;
        if (instance.budgetFrame != frame)
        {
            instance.budgetFrame = frame;
            instance.spawnsThisFrame = 0;
        }

        if (instance.spawnsThisFrame >= instance.maxSpawnsPerFrame) return false;
        instance.spawnsThisFrame++;
        return true;
    }

    private void Update()
    {
        if (queue.Count == 0) return;

        float start = Time.realtimeSinceStartup;
        int executed = 0;

        while (executed < maxActionsPerFrame && queue.Count > 0)
        {
            // limit czasu – resztę zostawiamy na następną klatkę
            if ((Time.realtimeSinceStartup - start) * 1000f >= maxTimePerFrameMs) break;

            WorkItem item = queue.Dequeue();

            // obiekt zniszczony albo wyłączony, zanim przyszła jego kolej
            if (item.owner == null || !item.owner.isActiveAndEnabled) continue;

            // błąd w jednym spawnerze nie blokuje pozostałych
            try
            {
                item.action();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e, item.owner);
            }

            executed++;
        }
    }
}