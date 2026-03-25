using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SpawnWorkQueue
{
    private class WorkItem
    {
        public MonoBehaviour owner;
        public System.Action action;
    }

    private const int MaxActionsPerFrame = 1;
    private const float MaxQueueTimePerFrameMs = 1.0f;

    private static readonly Queue<WorkItem> queue = new Queue<WorkItem>();
    private static bool isProcessing;

    public static void Enqueue(MonoBehaviour owner, System.Action action)
    {
        if (owner == null || action == null)
            return;

        queue.Enqueue(new WorkItem
        {
            owner = owner,
            action = action
        });

        if (isProcessing)
            return;

        MonoBehaviour runner = SingleObjectPool.Instance != null ? (MonoBehaviour)SingleObjectPool.Instance : owner;
        runner.StartCoroutine(ProcessQueue());
    }

    private static IEnumerator ProcessQueue()
    {
        isProcessing = true;

        while (queue.Count > 0)
        {
            float frameStart = Time.realtimeSinceStartup;
            int executed = 0;

            while (executed < MaxActionsPerFrame && queue.Count > 0)
            {
                float elapsedMs = (Time.realtimeSinceStartup - frameStart) * 1000f;
                if (elapsedMs >= MaxQueueTimePerFrameMs)
                    break;

                WorkItem item = queue.Dequeue();
                if (item.owner == null || !item.owner.isActiveAndEnabled)
                    continue;

                item.action();
                executed++;
            }

            if (queue.Count > 0)
                yield return null;
        }

        isProcessing = false;
    }
}
