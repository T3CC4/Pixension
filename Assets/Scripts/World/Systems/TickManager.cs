using System.Collections.Generic;
using UnityEngine;

namespace Pixension.Systems
{
    public interface ITickable
    {
        void OnTick();
    }

    public class TickManager : MonoBehaviour
    {
        private static TickManager instance;
        public static TickManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("TickManager");
                    instance = go.AddComponent<TickManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        [Header("Tick Settings")]
        public float tickRate = 0.1f; // Ticks per second (10 ticks/sec)
        public int ticksPerSecond = 10;

        private float tickTimer = 0f;
        private List<ITickable> tickables = new List<ITickable>();
        private long currentTick = 0;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            UpdateTickRate();
        }

        private void Update()
        {
            tickTimer += Time.deltaTime;

            while (tickTimer >= tickRate)
            {
                tickTimer -= tickRate;
                Tick();
            }
        }

        private void Tick()
        {
            currentTick++;

            // Process all tickable systems
            for (int i = tickables.Count - 1; i >= 0; i--)
            {
                if (tickables[i] == null)
                {
                    tickables.RemoveAt(i);
                    continue;
                }

                try
                {
                    tickables[i].OnTick();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error during tick for {tickables[i].GetType().Name}: {e.Message}");
                }
            }
        }

        public void RegisterTickable(ITickable tickable)
        {
            if (!tickables.Contains(tickable))
            {
                tickables.Add(tickable);
            }
        }

        public void UnregisterTickable(ITickable tickable)
        {
            tickables.Remove(tickable);
        }

        public void SetTicksPerSecond(int tps)
        {
            ticksPerSecond = tps;
            UpdateTickRate();
        }

        private void UpdateTickRate()
        {
            tickRate = 1f / ticksPerSecond;
        }

        public long GetCurrentTick()
        {
            return currentTick;
        }
    }
}
