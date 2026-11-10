using System;
using UnityEngine;

namespace Madness.Mods
{
    /// <summary>
    /// Mono DLL modları için temel sınıf.
    /// Bu sınıftan türeyen modlar Mono'da tüm sınırları aşabilir:
    /// yeni MonoBehaviour, yeni sistem, sahne, prefab, diyalog, kayıt vb.
    /// </summary>
    public abstract class ModModuleBase : IModModule
    {
        public abstract string Id { get; }

        public virtual void Register(ModRuntime runtime)
        {
            if (!ModAPI.IsFullPower)
            {
                Debug.LogWarning("[" + Id + "] Bu mod sadece Mono'da tam çalışır.");
            }
            OnRegister(runtime);
        }

        public virtual void Unregister(ModRuntime runtime)
        {
            OnUnregister(runtime);
            ModEventBus.UnsubscribeOwner(Id);
        }

        protected abstract void OnRegister(ModRuntime runtime);
        protected virtual void OnUnregister(ModRuntime runtime) { }

        protected void Subscribe<T>(Action<T> handler, int priority = 0) where T : ModContext
            => ModAPI.Subscribe(handler, priority, Id);

        protected void Log(string message) => ModAPI.Log(Id, message);
        protected void LogError(string message) => ModAPI.LogError(Id, message);

        protected void SetState(string json, int schema = 1) => ModAPI.SetState(Id, json, schema);
        protected string GetState() => ModAPI.GetState(Id);

        protected T LoadAsset<T>(string assetName) where T : UnityEngine.Object
            => ModAPI.LoadAsset<T>(Id, assetName);

        protected void RegisterContent(string contentId, UnityEngine.Object asset)
            => ModAPI.RegisterContent(contentId, asset);
    }
}
