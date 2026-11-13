using System;
using System.Collections.Generic;
using UnityEngine;

namespace Madness.Mods
{
    [Serializable]
    public sealed class ModManifest
    {
        public string id = "";
        public string name = "";
        public string description = "";
        public string version = "1.0.0";
        public string apiVersion = "1";
        public string author = "";
        // Manifest bu alanı içermiyorsa mod güvenli biçimde kapalı başlar.
        public bool enabled = false;
        public int loadOrder = 0;
        public string entryScript = "";
        public string worldScene = "";
        // totalConversion = true ise ModTotalConversion bu modun sahnesini oyun başlangıcında yükler.
        public bool totalConversion = false;
        // totalConversion için giriş sahnesi. Boşsa worldScene kullanılır.
        public string mainScene = "";
        public List<string> dependencies = new List<string>();
        public List<ModSettingDefinition> settings = new List<ModSettingDefinition>();
    }

    [Serializable]
    public sealed class ModSettingDefinition
    {
        // Desteklenen tipler: bool, int, float, string, dropdown.
        public string key = "";
        public string label = "";
        public string type = "float";
        public string defaultValue = "";
        public float min = 0f;
        public float max = 100f;
        public List<string> options = new List<string>();
        public bool restartRequired = true;
    }

    [Serializable]
    public sealed class ModSettingValue
    {
        public string key = "";
        public string value = "";
    }

    [Serializable]
    public sealed class ModSettingsFile
    {
        public List<ModSettingValue> values = new List<ModSettingValue>();
    }

    [Serializable]
    public sealed class ModDiagnostic
    {
        public string modId = "";
        public string message = "";
        public bool error;
    }

    [Serializable]
    public sealed class ModSaveEntry
    {
        public string modId = "";
        public int schemaVersion = 1;
        public string stateJson = "{}";
    }

    public interface IModModule
    {
        string Id { get; }
        void Register(ModRuntime runtime);
        void Unregister(ModRuntime runtime);
    }

    // Eski DLL modları bozulmadan kayıt yükleme bildirimini destekler.
    public interface IModSaveAware
    {
        void OnSaveLoaded(ModRuntime runtime);
        void OnBeforeSave(ModRuntime runtime);
    }

    public abstract class ModContext
    {
        public string SourceModId { get; internal set; }
        public bool Cancelled { get; set; }
        public string CancelReason { get; set; }
    }

    public sealed class PhaseChangedContext : ModContext
    {
        public GamePhase PreviousPhase;
        public GamePhase NextPhase;
    }

    public sealed class DayContext : ModContext
    {
        public int Day;
        public float DurationSeconds;
    }

    public sealed class NightContext : ModContext
    {
        public int Day;
        public bool IsBossNight;
        public float PoolMultiplier = 1f;
        public float SpawnIntervalMultiplier = 1f;
    }

    public sealed class GoldContext : ModContext
    {
        public int Amount;
        public string Reason;
    }

    public sealed class SaleContext : ModContext
    {
        public CustomerController Customer;
        public BookData Book;
        public int Price;
        public bool Served;
    }

    public sealed class CustomerSpawnContext : ModContext
    {
        public CustomerData Profile;
        public GameObject Prefab;
        public Vector3 SpawnPosition;
        public Vector3 TargetPosition;
        // Geriye dönük uyumluluk için tutulur; yeni modlar Cancelled kullanmalıdır.
        public bool CancelSpawn;
    }

    public sealed class EnemySpawnContext : ModContext
    {
        public EnemyType EnemyType;
        public int Day;
        public Vector3 SpawnPosition;
        public float HealthMultiplier = 1f;
        public float DamageMultiplier = 1f;
        // Geriye dönük uyumluluk için tutulur; yeni modlar Cancelled kullanmalıdır.
        public bool CancelSpawn;
        public bool IsElite;
    }

    public sealed class EnemyKilledContext : ModContext
    {
        public EnemyType EnemyType;
        public bool WasElite;
        public Vector3 Position;
    }

    public sealed class BossSpawnContext : ModContext
    {
        public int Day;
        public float HealthMultiplier = 1f;
        public float DamageMultiplier = 1f;
    }

    public sealed class DamageContext : ModContext
    {
        public object Target;
        public object Source;
        public float Amount;
        public bool InvulnerabilityApplies = true;
    }

    public sealed class WorldLoadContext : ModContext
    {
        public string WorldId;
        public string SceneName;
        public bool Additive;
        public bool CancelLoad;
    }
}
