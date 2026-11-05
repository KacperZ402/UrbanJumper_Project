using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

// ===================== DANE ZAPISU =====================
// Wszystko, co ma przetrwać między uruchomieniami gry.
// Nowe pola możesz dopisywać śmiało – stare zapisy dostaną wartość domyślną.
[Serializable]
public class SaveData
{
    public int version = GameSaver.CurrentVersion;
    public long lastSaveTime;            // unix time (sekundy) – przyda się do synchronizacji z chmurą

    // Postęp
    public int highScore;
    public int coins;
    public int totalRuns;

    // Skiny / sklep
    public List<string> unlockedSkins = new List<string> { "default" };
    public string selectedSkin = "default";

    // Ustawienia
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    public bool vibration = true;
}

// ===================== MENEDŻER ZAPISU =====================
// Wrzuć na pusty obiekt "GameSaver" w PIERWSZEJ scenie (np. menu).
// Obiekt musi być na samej górze hierarchii (nie jako dziecko), inaczej DontDestroyOnLoad nie zadziała.
public class GameSaver : MonoBehaviour
{
    public const int CurrentVersion = 1;

    public static GameSaver Instance { get; private set; }
    public SaveData Data { get; private set; }

    // Wywoływane po każdej zmianie danych – np. menu może odświeżyć licznik monet
    public event Action OnDataChanged;

    // ZMIEŃ na własny, długi, losowy ciąg znaków (i nie zmieniaj go po wydaniu gry!)
    private const string Secret = "fgdflfsdiwpbbhswpxmzap[mlcjei345";
    private const string FileName = "save.json";
    private const string LegacyHighScoreKey = "HighScoreMeters"; // stary klucz z PlayerPrefs

    private string SavePath => System.IO.Path.Combine(Application.persistentDataPath, FileName);
    private string TempPath => SavePath + ".tmp";
    private string BackupPath => SavePath + ".bak";

    // Format pliku na dysku: dane w base64 + podpis
    [Serializable]
    private class SaveFile
    {
        public string data;
        public string sig;
    }

    // ---------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
        Debug.Log($"[GameSaver] Wczytano: monety={Data.coins}, rekord={Data.highScore}");
    }

    private void OnApplicationPause(bool paused)
    {
        // Na Androidzie to najpewniejszy moment zapisu – OnApplicationQuit często się nie wywołuje
        if (paused) Save();
    }

    private void OnApplicationQuit() => Save();

    // ===================== API DLA RESZTY GRY =====================

    /// <summary>
    /// Zapisuje wynik biegu. coinsToAdd = monety, które jeszcze NIE trafiły do portfela.
    /// Zwraca true, jeśli padł nowy rekord.
    /// </summary>
    public bool SubmitRun(int distance, int coinsToAdd, bool countRun = true)
    {
        bool newRecord = distance > Data.highScore;
        if (newRecord) Data.highScore = distance;

        Data.coins += Mathf.Max(0, coinsToAdd);
        if (countRun) Data.totalRuns++;

        SaveAndNotify();
        return newRecord;
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        Data.coins += amount;
        SaveAndNotify();
    }

    /// <summary>Do sklepu. Zwraca false, jeśli gracza nie stać.</summary>
    public bool TrySpendCoins(int amount)
    {
        if (amount < 0 || Data.coins < amount) return false;
        Data.coins -= amount;
        SaveAndNotify();
        return true;
    }

    public bool IsSkinUnlocked(string skinId) => Data.unlockedSkins.Contains(skinId);

    public void UnlockSkin(string skinId)
    {
        if (IsSkinUnlocked(skinId)) return;
        Data.unlockedSkins.Add(skinId);
        SaveAndNotify();
    }

    public void SelectSkin(string skinId)
    {
        if (!IsSkinUnlocked(skinId)) return;
        Data.selectedSkin = skinId;
        SaveAndNotify();
    }

    // Ustawienia – zapisują się dopiero przy wyjściu/pauzie (suwak wywołuje to wiele razy na sekundę)
    public void SetMusicVolume(float v) { Data.musicVolume = Mathf.Clamp01(v); OnDataChanged?.Invoke(); }
    public void SetSfxVolume(float v) { Data.sfxVolume = Mathf.Clamp01(v); OnDataChanged?.Invoke(); }
    public void SetVibration(bool on) { Data.vibration = on; SaveAndNotify(); }

    // ===================== ZAPIS / ODCZYT =====================

    public void Save()
    {
        if (Data == null) return;

        try
        {
            Data.version = CurrentVersion;
            Data.lastSaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            string json = JsonUtility.ToJson(Data);
            var file = new SaveFile
            {
                data = Convert.ToBase64String(Encoding.UTF8.GetBytes(json)),
                sig = Sign(json)
            };

            // Najpierw zapis do pliku tymczasowego, potem podmiana –
            // crash w trakcie zapisu nie zniszczy poprzedniego save'a
            System.IO.File.WriteAllText(TempPath, JsonUtility.ToJson(file));

            if (System.IO.File.Exists(SavePath))
            {
                System.IO.File.Copy(SavePath, BackupPath, true); // kopia zapasowa
                System.IO.File.Delete(SavePath);
            }
            System.IO.File.Move(TempPath, SavePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameSaver] Błąd zapisu: {e.Message}");
        }
    }

    public void Load()
    {
        if (TryReadFile(SavePath, out SaveData loaded) || TryReadFile(BackupPath, out loaded))
        {
            Data = loaded;
        }
        else
        {
            bool hadFile = System.IO.File.Exists(SavePath) || System.IO.File.Exists(BackupPath);
            if (hadFile)
                Debug.LogWarning("[GameSaver] Zapis uszkodzony lub zmodyfikowany – start od zera.");

            Data = new SaveData();
        }

        OnDataChanged?.Invoke();
    }

    private bool TryReadFile(string path, out SaveData result)
    {
        result = null;
        try
        {
            if (!System.IO.File.Exists(path)) return false;

            var file = JsonUtility.FromJson<SaveFile>(System.IO.File.ReadAllText(path));
            if (file == null || string.IsNullOrEmpty(file.data)) return false;

            string json = Encoding.UTF8.GetString(Convert.FromBase64String(file.data));
            if (Sign(json) != file.sig) return false; // ktoś grzebał w pliku

            result = JsonUtility.FromJson<SaveData>(json);
            return result != null;
        }
        catch
        {
            return false;
        }
    }

    private string Sign(string text)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret)))
        {
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }
    }

    private void SaveAndNotify()
    {
        Save();
        OnDataChanged?.Invoke();
    }

    // ===================== NARZĘDZIA DO TESTÓW =====================
    // Prawy klik na komponencie GameSaver w Inspectorze

    [ContextMenu("Reset zapisu")]
    public void ResetSave()
    {
        Data = new SaveData();
        if (System.IO.File.Exists(SavePath)) System.IO.File.Delete(SavePath);
        if (System.IO.File.Exists(BackupPath)) System.IO.File.Delete(BackupPath);
        Save();
        OnDataChanged?.Invoke();
        Debug.Log("[GameSaver] Zapis zresetowany.");
    }

    [ContextMenu("Pokaż folder zapisu")]
    private void OpenSaveFolder()
    {
        Debug.Log($"[GameSaver] {Application.persistentDataPath}");
        Application.OpenURL("file://" + Application.persistentDataPath);
    }

    [ContextMenu("Dodaj 1000 monet (test)")]
    private void DebugAddCoins() => AddCoins(1000);
}