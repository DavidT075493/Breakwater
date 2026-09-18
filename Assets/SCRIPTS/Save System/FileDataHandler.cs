using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Linq;
using UnityEngine.Playables;

public class FileDataHandler
{
    string dataDirPath = "";
    string dataFileName = "";
    bool useEncryption = true;
    private readonly string encryptionCodeWord = "ligma";
    string fileFolderName = " save files";

    const string encryptedPrefix = "THIS SAVE FILE IS ENCRYPTED, LOSER\n\n\n";

    public FileDataHandler(string dataDirPath, string dataFileName, bool encrypt)
    {
        this.dataDirPath = dataDirPath;
        this.dataFileName = dataFileName;
        this.useEncryption = encrypt;
    }

    public GameData Load(string profileId)
    {
        string fullPath = GetFullPath(profileId);
        GameData loadedData = null;

        if (File.Exists(fullPath))
        {
            try
            {
                string dataToLoad = "";

                using (FileStream stream = new FileStream(fullPath, FileMode.Open))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        dataToLoad = reader.ReadToEnd();
                    }
                }

                if (dataToLoad.StartsWith(encryptedPrefix))
                {
                    dataToLoad = dataToLoad.Substring(encryptedPrefix.Length); // Remove the tag
                    dataToLoad = EncryptDecrypt(dataToLoad); // Decrypt
                }

                loadedData = JsonUtility.FromJson<GameData>(dataToLoad);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error while reading data from file: " + fullPath + "\n" + ex);
            }
        }

        return loadedData;
    }


    string GetFullPath(string profileId)
    {
        return Path.Combine(dataDirPath, PlayerPrefs.GetString("userID") + fileFolderName, profileId, DataPersistanceManager.gameOwnerID + dataFileName);
    }


    public void Save(GameData data, string profileId)
    {
        string fullPath = GetFullPath(profileId);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            string dataToStore = JsonUtility.ToJson(data, true);

            if (useEncryption)
            {
                dataToStore = encryptedPrefix + EncryptDecrypt(dataToStore);
            }

            using (FileStream stream = new FileStream(fullPath, FileMode.Create))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(dataToStore);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error while saving data to file: " + fullPath + "\n" + ex);
        }
    }

    public void Delete(string profileId)
    {
        if (profileId == null)
            return;

        string fullPath = GetFullPath(profileId);
        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            else
            {
                Debug.LogWarning("Data can't be deleted because it doesn't exist " + fullPath);
            }

        }
        catch (Exception e)
        {
            Debug.LogError("Failed to delete profile data\n" + e);
        }


    }



    public SerializableDictionary<string,GameData> LoadAllProfiles()
    {
        SerializableDictionary<string, GameData> profileDictionary = new SerializableDictionary<string, GameData>();

        string fullPath = Path.Combine(dataDirPath, PlayerPrefs.GetString("userID") + fileFolderName);

        if (!Directory.Exists(fullPath))
        Directory.CreateDirectory(fullPath);

        IEnumerable<DirectoryInfo> dirInfos = new DirectoryInfo(fullPath).EnumerateDirectories();
        //Debug.Log(dirInfos.Count());
        foreach (DirectoryInfo dirInfo in dirInfos)
        {
            string profileId = dirInfo.Name;

            string saveFilePath = GetFullPath(profileId);
            if (!File.Exists(saveFilePath))
            {
                continue;
            }


            GameData profileData = Load(profileId);

            if(profileData != null)
            {
                profileDictionary.Add(profileId, profileData);
            }

            if (profileData.fileName == "" || profileData.fileName == null)
            {
                profileData.fileName = "File " + profileId;
            }

        }

        return profileDictionary;

    }


    string EncryptDecrypt(string data)
    {
        System.Text.StringBuilder modifiedData = new System.Text.StringBuilder();

        for (int i = 0; i < data.Length; i++)
        {
            modifiedData.Append((char)(data[i] ^ encryptionCodeWord[i % encryptionCodeWord.Length]));
        }

        return modifiedData.ToString();
    }



}
