using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Wspólna baza spawnerów propów:
// - propy spawnują się raz, przy pierwszym włączeniu (segment z poola wraca z tym samym układem),
// - plan liczony od razu na głównym wątku,
// - spawn rozłożony na klatki przez globalny budżet z SpawnWorkQueue,
// - propy lądują w kontenerze "SpawnedProps".
public abstract class PropSpawnerBase : MonoBehaviour
{
    protected struct SpawnRequest
    {
        public GameObject prefab;
        public Vector3 position;
        public Quaternion rotation;

        public SpawnRequest(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            this.prefab = prefab;
            this.position = position;
            this.rotation = rotation;
        }
    }

    private Transform container;
    private bool hasSpawned;

    private readonly List<SpawnRequest> plan = new List<SpawnRequest>();

    // Czy spawner sam startuje po włączeniu (Wall czeka na event z podłogi)
    protected virtual bool AutoSpawnOnEnable => true;

    protected Transform Container
    {
        get
        {
            if (container == null)
            {
                container = new GameObject("SpawnedProps").transform;
                container.SetParent(transform, false);
            }
            return container;
        }
    }

    protected virtual void OnEnable()
    {
        if (!AutoSpawnOnEnable || hasSpawned) return;

        SpawnWorkQueue.Enqueue(this, Spawn);
    }

    // Spawnuje propy tylko raz – przy kolejnych użyciach obiekt zostaje z tym samym układem
    public void Spawn()
    {
        if (hasSpawned) return;
        hasSpawned = true;

        plan.Clear();
        BuildPlan(plan);

        if (plan.Count > 0)
            StartCoroutine(SpawnRoutine());
    }

    // Tu konkretny spawner mówi: co, gdzie i z jaką rotacją postawić
    protected abstract void BuildPlan(List<SpawnRequest> plan);

    private IEnumerator SpawnRoutine()
    {
        if (SingleObjectPool.Instance == null)
        {
            Debug.LogError("[PropSpawnerBase] Brak SingleObjectPool w scenie.", this);
            yield break;
        }

        for (int i = 0; i < plan.Count; i++)
        {
            // wspólny limit spawnów na klatkę dla wszystkich spawnerów
            while (!SpawnWorkQueue.TryConsumeSpawn())
                yield return null;

            SpawnRequest r = plan[i];
            if (r.prefab == null) continue;

            GameObject obj = SingleObjectPool.Instance.Get(r.prefab, r.position, r.rotation, Container);

            // skala jak przy świeżym Instantiate – obiekt z poola mógł mieć inną po poprzednim rodzicu
            if (obj != null)
                obj.transform.localScale = r.prefab.transform.localScale;
        }

        plan.Clear(); // plan nie jest już potrzebny
    }

    // Losowy punkt na górnej ściance BoxCollidera – liczony z transformu,
    // a nie z collider.bounds, które zależy od stanu fizyki
    protected static Vector3 RandomPointOnTop(BoxCollider box)
    {
        Vector3 half = box.size * 0.5f;
        Vector3 local = box.center + new Vector3(
            Random.Range(-half.x, half.x),
            half.y,
            Random.Range(-half.z, half.z));

        return box.transform.TransformPoint(local);
    }

    // AABB w świecie liczony z transformu – to samo co collider.bounds, ale bez zależności od fizyki
    protected static Bounds GetWorldBounds(BoxCollider box)
    {
        Transform t = box.transform;
        Vector3 e = box.size * 0.5f;
        Vector3 ax = t.TransformVector(e.x, 0f, 0f);
        Vector3 ay = t.TransformVector(0f, e.y, 0f);
        Vector3 az = t.TransformVector(0f, 0f, e.z);

        Vector3 extents = new Vector3(
            Mathf.Abs(ax.x) + Mathf.Abs(ay.x) + Mathf.Abs(az.x),
            Mathf.Abs(ax.y) + Mathf.Abs(ay.y) + Mathf.Abs(az.y),
            Mathf.Abs(ax.z) + Mathf.Abs(ay.z) + Mathf.Abs(az.z));

        return new Bounds(t.TransformPoint(box.center), extents * 2f);
    }
}