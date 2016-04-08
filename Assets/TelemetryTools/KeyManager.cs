#define POSTENABLED

using System;
using UnityEngine;

using BytesPerSecond = System.Single;
using Bytes = System.UInt32;
using Megabytes = System.UInt32;
using Milliseconds = System.Int64;
using FilePath = System.String;
using URL = System.String;
using SequenceID = System.Nullable<System.UInt32>;
using SessionID = System.Nullable<System.UInt32>;
using KeyID = System.Nullable<System.UInt32>;
using FrameID = System.UInt32;
using UserDataKey = System.String;
using UniqueKey = System.String;
using System.Collections.Generic;


namespace TelemetryTools
{
    public class KeyManager
    {
        private Telemetry telemetry;

        private UniqueKey[] keys;
        public UniqueKey[] Keys { get { return keys; } }
        public int NumberOfKeys { get { if (keys != null) return keys.Length; else return 0; } }
        private uint usedKeys;
        public uint NumberOfUsedKeys { get { return usedKeys; } }
        private KeyID currentKeyID;
        public KeyID CurrentKeyID { get { return currentKeyID; } }
        public UniqueKey CurrentKey { get { if (keys != null) if (currentKeyID != null) if (currentKeyID < keys.Length) return keys[(int)currentKeyID]; return ""; } }

        private URL keyServer;
        public URL KeyServer { get { return keyServer; } set { keyServer = value; } }

        private WWW keywww;
        private bool keywwwBusy;

        private const Milliseconds requestKeyDelayOnFailure = 10000;

        /// <summary>
        /// Returns true if the we have a currentKeyID set.
        /// </summary>
        public bool UsingKey { get  { return currentKeyID != null; } }

        /// <summary>
        /// Returns true if the currentKeyID corresponds to a key we have fetched.
        /// </summary>
        public bool HasKey
        {
            get
            {
                if (UsingKey)
                    return CurrentKeyID < NumberOfKeys;
                return false;
            }
        }

        public KeyManager(Telemetry telemetry, URL keyServer)
        {
            this.telemetry = telemetry;
            keys = new string[0];

#if POSTENABLED
            this.keyServer = keyServer;

            int numKeys = 0;
            if (Int32.TryParse(PlayerPrefs.GetString("numkeys"), out numKeys))
                keys = new string[numKeys];

            int usedKeysParsed = 0;
            Int32.TryParse(PlayerPrefs.GetString("usedkeys"), out usedKeysParsed);
            usedKeys = (uint) usedKeysParsed;

            currentKeyID = null;
            
            for (int i = 0; i < NumberOfKeys; i++)
                keys[i] = PlayerPrefs.GetString("key" + i);
#endif
        }

        public void Update(bool httpPostEnabled)
        {
            if (httpPostEnabled)
                if (ConnectionLogger.Instance.RequestKeyDelay <= 0)
                    RequestKeyIfNone(UserProperties);
        }

        public static KeyValuePair<UserDataKey, string>[] UserProperties
        {
            get
            {
                List<KeyValuePair<UserDataKey, string>> userData = new List<KeyValuePair<UserDataKey, string>>();
                userData.Add(new KeyValuePair<UserDataKey, string>(UserPropertyKeys.Platform, Application.platform.ToString()));
                userData.Add(new KeyValuePair<UserDataKey, string>(UserPropertyKeys.Version, Application.version));
                userData.Add(new KeyValuePair<UserDataKey, string>(UserPropertyKeys.UnityVersion, Application.unityVersion));
                userData.Add(new KeyValuePair<UserDataKey, string>(UserPropertyKeys.Genuine, Application.genuine.ToString()));
                if (Application.isWebPlayer)
                    userData.Add(new KeyValuePair<UserDataKey, string>(UserPropertyKeys.WebPlayerURL, Application.absoluteURL));

                return userData.ToArray();
            }
        }

        public UniqueKey GetKeyByID(KeyID id)
        {
            return keys[(uint) id];
        }

        public bool KeyIsValid(KeyID id)
        {
            return id < NumberOfKeys;
        }

        public void HandleKeyWWWResponse()
        {
            bool? success = null;
            if (keywwwBusy)
            {
                if (keywww != null)
                {
                    UniqueKey newKey = null;
                    success = GetReturnedKey(ref keywww, ref newKey);
                    if (success != null)
                    {
                        if (success == true)
                        {
                            ConnectionLogger.Instance.KeyServerSuccess();
                            Array.Resize(ref keys, NumberOfKeys + 1);
                            keys[NumberOfKeys - 1] = newKey;
                            PlayerPrefs.SetString("key" + (NumberOfKeys - 1), newKey);
                            PlayerPrefs.SetString("numkeys", NumberOfKeys.ToString());
                            PlayerPrefs.Save();
                            ConnectionLogger.Instance.UploadUserDataDelay = 0;
                            ConnectionLogger.Instance.UploadCacheFilesDelay = 0;
                        }
                        else
                        {
                            ConnectionLogger.Instance.KeyServerError();
                            ConnectionLogger.Instance.RequestKeyDelay = requestKeyDelayOnFailure;
                        }
                        keywwwBusy = false;
                    }
                }
            }
        }

        public void ChangeKey()
        {
            usedKeys++;
            ChangeKey(usedKeys - 1, newKey: true);
        }

        public void ChangeKey(uint key, bool newKey = false)
        {
            if (key < usedKeys)
            {
                telemetry.SaveUserData();
                telemetry.SendAllBuffered();

                currentKeyID = key;
                if (!newKey)
                    telemetry.UserData = Telemetry.LoadUserData(currentKeyID);
                else
                    telemetry.UserData = new Dictionary<UserDataKey, string>();

                PlayerPrefs.SetString("currentkeyid", currentKeyID.ToString());
                PlayerPrefs.SetString("usedkeys", usedKeys.ToString());
                PlayerPrefs.Save();

                telemetry.Restart();
            }
        }

        private void RequestKeyIfNone(KeyValuePair<UserDataKey, string>[] userData)
        {
            if (!keywwwBusy)
                if (usedKeys > NumberOfKeys)
                {
                    keywww = RequestUniqueKey(this.keyServer, userData, ref keywwwBusy);
                    ConnectionLogger.Instance.KeyServerRequestSent();
                }
        }

        private static WWW RequestUniqueKey(URL keyServer, KeyValuePair<string, string>[] userData, ref bool keyWWWBusy)
        {
            WWWForm form = new WWWForm();
            form.AddField(UserPropertyKeys.RequestTime, System.DateTime.UtcNow.ToString("u"));

            foreach (KeyValuePair<string, string> pair in userData)
                form.AddField(pair.Key, pair.Value);

            keyWWWBusy = true;
            return new WWW(keyServer, form);
        }

        private static bool? GetReturnedKey(ref WWW keywww, ref string uniqueKey)
        {
            if (keywww != null)
                if (keywww.isDone)
                {
                    if (string.IsNullOrEmpty(keywww.error))
                    {
                        if (keywww.text.StartsWith("key:"))
                        {
                            uniqueKey = keywww.text.Substring(4);
                            Debug.Log("Key retrieved: " + uniqueKey);
                            return true;
                        }
                        else
                        {
                            Debug.LogWarning("Invalid key retrieved: " + keywww.text);
                            return false;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Error connecting to key server");
                        return false;
                    }
                }
            return null;
        }
    }
}