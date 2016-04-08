#if (!UNITY_WEBPLAYER)
#define LOCALSAVEENABLED
#endif

#define POSTENABLED

using System;
using System.Text;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

#if LOCALSAVEENABLED
using System.IO;
#endif

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

namespace TelemetryTools
{
    /// <summary>
    /// This class contains pre-defined event strings for basic system events.
    /// </summary>
    public static class Event
    {
        public const string TelemetryStart = "TTStart";
        public const string Frame = "Frame";
        public const string ApplicationPause = "AppPause";
        public const string ApplicationUnpause = "AppUnpause";
        public const string ApplicationQuit = "AppQuit";
    }

    /// <summary>
    /// This class contains pre-defined stream value keys for common system values.
    /// </summary>
    public static class Stream
    {
        public const string FrameTime = "FT";
        public const string DeltaTime = "DT";
        public const string LostData = "LD";
    }

    /// <summary>
    /// This class contains pre-defined keys for the user properties sumbitted with a key request.
    /// </summary>
    public static class UserPropertyKeys
    {
        public const string RequestTime = "RequestTime";
        public const string Platform = "Platform";
        public const string WebPlayerURL = "WebPlayerURL";
        public const string Version = "Version";
        public const string UnityVersion = "UnityVersion";
        public const string Genuine = "Genuine";
    }

    public class Telemetry
    {
        /// <summary>
        /// This provides static access to a singleton instance of Telemetry. If the singleton does not yet exists, this creates it. Most interaction with TelemetryTools is via this static accessor.
        /// </summary>
        public static Telemetry Instance
        {
            get
            {
                if (instance == null)
                    instance = new Telemetry(); 
                return instance;
            }
        }
        private static Telemetry instance;

        /// <summary>
        /// Returns true if a Telemetry instance exists.
        /// </summary>
        public static bool Exists { get { return instance != null; } }

        private Milliseconds startTime = 0;
        private readonly SessionID sessionID = 0;
        private SequenceID sequenceID = 0;
        private FrameID frameID = 0;

        private const FilePath fileExtension = "telemetry";
#if LOCALSAVEENABLED
        // Local
        private const FilePath cacheDirectory = "cache";
        private const FilePath cacheListFilename = "cache.txt";
        private List<FilePath> cachedFilesList;
#endif
        private const Milliseconds uploadCachedFilesDelayOnFailure = 10000;

#if POSTENABLED 
        // Remote
        private bool httpPostEnabled = false;
        public bool HTTPPostEnabled { get { return httpPostEnabled; } set { httpPostEnabled = value; } }
        private URL uploadURL;
        public URL UploadURL { get { return uploadURL; } set { uploadURL = value; } }

        private WWW www;
        private byte[] wwwData;
        private SequenceID wwwSequenceID;
        private SessionID wwwSessionID;
        private bool wwwBusy = false;
        private UniqueKey wwwKey;
        private KeyID wwwKeyID;

        //User Data
        private WWW userDatawww;
        private URL userDataURL;
        public URL UserDataURL { get { return userDataURL; } set { userDataURL = value; } }
        private bool userDatawwwBusy;
        private KeyID userDatawwwKeyID;
#endif
        private Dictionary<UserDataKey,string> userData;
        public Dictionary<UserDataKey, string> UserData { get { return userData; } set { userData = value; } }
        private const FilePath userDataDirectory = "userdata";
        private const FilePath userDataFileExtension = "userdata";
        private const FilePath userDataListFilename = "userdata.txt";
        private List<FilePath> userDataFilesList;
        public List<FilePath> UserDataFilesList { get { return userDataFilesList; } }
        public int UserDataFiles { get { if (userDataFilesList != null) return userDataFilesList.Count; return 0; } }

        private const Milliseconds uploadUserDataDelayOnFailure = 10000;

        private readonly Bytes minSendingThreshold;
        private const Bytes defaultMinSendingThreshold = 1024;

        // Buffers
        private const Bytes defaultBufferSize = 1048576;
        private const Bytes defaultFrameBufferSize = 1024*128;
        private readonly Bytes bufferSize;
        private byte[] outboxBuffer1;
        private byte[] outboxBuffer2;
        private byte[] ActiveBuffer { get { if (buffer1Active) return outboxBuffer1; else return outboxBuffer2; } }
        private byte[] OffBuffer { get { if (buffer1Active) return outboxBuffer2; else return outboxBuffer1; } }
        private int bufferPos = 0;
        private bool buffer1Active = true;
        private bool offBufferFull = false;
        private byte[] frameBuffer;
        private int frameBufferPos = 0;

        private KeyManager keyManager;
        public KeyManager KeyManager { get { return keyManager; } }

        public int CachedFiles
        {
            get
            {
#if LOCALSAVEENABLED
                if (cachedFilesList != null)
                    return cachedFilesList.Count;
                else
#endif
                    return 0;
            }
        }


        public Telemetry(URL uploadURL = "", URL keyServer = "", URL userDataURL = "", Bytes bufferSize = defaultBufferSize, Bytes frameBufferSize = defaultFrameBufferSize, Bytes minSendingThreshold = defaultMinSendingThreshold)
        {
            this.bufferSize = bufferSize;
            this.minSendingThreshold = minSendingThreshold;

            outboxBuffer1 = new byte[bufferSize];
            outboxBuffer2 = new byte[bufferSize];
            frameBuffer = new byte[frameBufferSize];

            Array.Clear(outboxBuffer1, 0, outboxBuffer1.Length);
            Array.Clear(outboxBuffer2, 0, outboxBuffer2.Length);
            Array.Clear(frameBuffer, 0, frameBuffer.Length);

            keyManager = new KeyManager(this, keyServer);

#if LOCALSAVEENABLED
            cachedFilesList = ReadStringsFromFile(GetFileInfo(cacheDirectory, cacheListFilename));
            userDataFilesList = ReadStringsFromFile(GetFileInfo(userDataDirectory, userDataListFilename));
#endif

            sessionID = (SessionID)PlayerPrefs.GetInt("sessionID");
            PlayerPrefs.SetInt("sessionID", (int)sessionID+1);
            PlayerPrefs.Save();

#if POSTENABLED
            this.uploadURL = uploadURL;
            this.userDataURL = userDataURL;
#endif

            Debug.Log("Persistant Data Path: " + Application.persistentDataPath);
        }

        public static void Update() { Instance.UpdateP(); }

        private void UpdateP()
        {
            if (offBufferFull)
                if (!wwwBusy)
                    offBufferFull = !SendBuffer(RemoveTrailingNulls(OffBuffer));
#if POSTENABLED
            keyManager.HandleKeyWWWResponse();
            HandleUserDataWWWResponse(ref userDatawww, ref userDatawwwBusy, ref userDatawwwKeyID, ref userData, userDatawwwKeyID, keyManager.CurrentKeyID, userDataFilesList);
            SaveDataOnWWWErrorIfWeCan();

            keyManager.Update(httpPostEnabled);

            if (httpPostEnabled)
            {
#if LOCALSAVEENABLED
                if ((!userDatawwwBusy) && (ConnectionLogger.Instance.UploadUserDataDelay <= 0))
                    if (!UploadBacklogOfUserData())
                        ConnectionLogger.Instance.UploadUserDataDelay = uploadUserDataDelayOnFailure;
                if ((!offBufferFull) && (!wwwBusy) && (ConnectionLogger.Instance.UploadCacheFilesDelay <= 0))
                    if (!UploadBacklogOfCacheFiles())
                        ConnectionLogger.Instance.UploadCacheFilesDelay = uploadCachedFilesDelayOnFailure;
#endif

                if ((!offBufferFull) && (!wwwBusy))
                    if (bufferPos > minSendingThreshold)
                        if (SendBuffer(GetDataInActiveBuffer(), httpOnly: true))
                            bufferPos = 0;
            }
#endif
            ConnectionLogger.Instance.Update();
        }

        public void WriteEverything()
        {
            SaveUserData(keyManager.CurrentKeyID, userData, userDataFilesList);

            SendFrame();
            byte[] dataInBuffer = GetDataInActiveBuffer();
            bool savedBuffer = false;

#if LOCALSAVEENABLED
            if (keyManager.CurrentKeyID != null)
            {
                if (dataInBuffer.Length > 0)
                    WriteCacheFile(dataInBuffer, sessionID, sequenceID, keyManager.CurrentKeyID);
                savedBuffer = true;
            }
#endif

            bufferPos = 0;
            sequenceID++;

#if POSTENABLED
            SaveDataOnWWWErrorIfWeCan();

            if (www != null)
            {
    #if LOCALSAVEENABLED
                if (!www.isDone)
                    WriteCacheFile(wwwData, wwwSessionID, wwwSequenceID, wwwKeyID);
    #endif
                DisposeWWW(ref www, ref wwwData, ref wwwSessionID, ref wwwSequenceID, ref wwwBusy, ref wwwKey, ref wwwKeyID);
            }

            /*if ((httpPostEnabled) && (!savedBuffer))
            {
                if (currentKeyID != null)
                    if (currentKeyID < NumberOfKeys)
                    {
                        WWW stopwww = null;
                        SendByHTTPPost(dataInBuffer, sessionID, sequenceID, fileExtension, keys[(uint) currentKeyID], currentKeyID, uploadURL, ref stopwww, out wwwData, out wwwSequenceID, out wwwSessionID, out wwwBusy, out wwwKey, out wwwKeyID, ref totalHTTPRequestsSent);
                        savedBuffer = true;
                    }
            }*/
#endif

            //if (!savedBuffer)
            //    lostData += (uint) dataInBuffer.Length;
        }




        public void UpdateUserData(UserDataKey key, string value)
        {
            if (keyManager.CurrentKeyID != null)
                userData[key] = value;
            else
                Debug.LogWarning("Cannot log user data without a unique key.");
        }

        private bool UploadUserData(KeyID key)
        {
            if (keyManager.HasKey)
            {
                if (key == keyManager.CurrentKeyID)
                    SendUserDataByHTTPPost(userDataURL, userData, keyManager.GetKeyByID(key), key, ref userDatawww, ref userDatawwwBusy, ref userDatawwwKeyID);
                else
                    SendUserDataByHTTPPost(userDataURL, LoadUserData(key), keyManager.GetKeyByID(key), key, ref userDatawww, ref userDatawwwBusy, ref userDatawwwKeyID);
                return true;
            }
            else
            {
                Debug.LogWarning("Cannot upload user data of keyID " + key + " because we have not yet fetched that key.");
                return false;
            }
        }

        public bool UploadBacklogOfUserData()
        {
            int i = 0;
            if (!userDatawwwBusy)
            {
                string[] separators = new string[1];
                separators[0] = ".";
                string[] strs = userDataFilesList[i].Split(separators, 2, System.StringSplitOptions.None);
                uint result = 0;
                if (UInt32.TryParse(strs[0], out result))
                    UploadUserData(result);
                else
                {
                    userDataFilesList.RemoveAt(i);
                    WriteStringsToFile(userDataFilesList.ToArray(), GetFileInfo(userDataDirectory, userDataListFilename));
                }

            }
            return userDatawwwBusy; // If www is busy, we successfully found something to upload
        }

        private bool UploadBacklogOfCacheFiles()
        {
            if (cachedFilesList.Count > 0)
            {
                byte[] data;
                SessionID snID;
                SequenceID sqID;
                KeyID keyID;
                int i = 0;
                if (!wwwBusy)
                {
                    ParseCacheFileName(cacheDirectory, cachedFilesList[i], out snID, out sqID, out keyID);
                    if (keyManager.KeyIsValid(keyID))
                    {
                        if (LoadFromCacheFile(cacheDirectory, cachedFilesList[i], out data))
                        {
                            if ((data.Length > 0) && (snID != null) && (sqID != null) && (keyID != null)) // key here could be empty because it was not known when the file was saved
                            {
                                SendByHTTPPost(data, snID, sqID, fileExtension, keyManager.GetKeyByID(keyID), keyID, uploadURL, ref www, out wwwData, out wwwSequenceID, out wwwSessionID, out wwwBusy, out wwwKey, out wwwKeyID);
                                File.Delete(GetFileInfo(cacheDirectory, cachedFilesList[i]).FullName);
                                cachedFilesList.RemoveAt(i);
                                WriteStringsToFile(cachedFilesList.ToArray(), GetFileInfo(cacheDirectory, cacheListFilename));
                            }
                            else
                            {
                                StringBuilder sb = new StringBuilder();
                                sb.Append("Values loaded from cache file seem to be invalid:");
                                if (data.Length <= 0)
                                    sb.Append("\n* Data loaded is empty");
                                if (snID == null)
                                    sb.Append("\n* Session ID is null");
                                if (sqID == null)
                                    sb.Append("\n* Sequence ID is null");
                                if (keyID == null)
                                    sb.Append("\n* Key ID is null");

                                Debug.LogWarning(sb.ToString());
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Error loading from cache file for KeyID:  " + (keyID == null ? "null" : keyID.ToString()));
                            File.Delete(GetFileInfo(cacheDirectory, cachedFilesList[i]).FullName);
                            cachedFilesList.RemoveAt(i);
                            WriteStringsToFile(cachedFilesList.ToArray(), GetFileInfo(cacheDirectory, cacheListFilename));
                        }
                    }
                    else
                        Debug.LogWarning("Cannot upload cache file because KeyID " + keyID.ToString() + " has not been retrieved from the key server.");
                }
            }
            return wwwBusy; // If www is busy, we successfully found something to upload
        }

        private static void SendUserDataByHTTPPost( URL userDataURL,
                                                    Dictionary<UserDataKey,string> userData,
                                                    UniqueKey uniqueKey,
                                                    KeyID keyID,
                                                    ref WWW userDatawww,
                                                    ref bool userDatawwwBusy,
                                                    ref KeyID userDatawwwKeyID)
        {
            if (!String.IsNullOrEmpty(uniqueKey))
            {
                if (userData.Count > 0)
                {
                    WWWForm form = new WWWForm();
                    form.AddField("key", uniqueKey);
                    foreach (string key in userData.Keys)
                        form.AddField(key, userData[key]);

                    userDatawww = new WWW(userDataURL, form);
                    userDatawwwBusy = true;
                    userDatawwwKeyID = keyID;
                    ConnectionLogger.Instance.HTTPRequestSent();
                }
                else
                    Debug.LogWarning("Cannot send empty user data to server");
            }
            else
                Debug.LogWarning("Cannot send user data to server without a key");
        }

        public void SaveUserData()
        {
            SaveUserData(keyManager.CurrentKeyID, userData, userDataFilesList);
        }

        private static void SaveUserData(KeyID currentKeyID, Dictionary<UserDataKey,string> userData, List<FilePath> userDataFilesList)
        {
            if (currentKeyID != null)
            {
                if (userData != null)
                {
                    if (userData.Count > 0)
                    {
                        string[] stringList = new string[userData.Keys.Count];
                        int i = 0;
                        foreach (string key in userData.Keys)
                        {
                            stringList[i] = key + "," + userData[key];
                            i++;
                        }

                        FileInfo file = GetFileInfo(userDataDirectory, currentKeyID.ToString() + "." + userDataFileExtension);
                        WriteStringsToFile(stringList, file);
                        userDataFilesList.Remove(file.Name);
                        userDataFilesList.Add(file.Name);
                        //TODO: Append rather than rewrite everything
                        WriteStringsToFile(userDataFilesList.ToArray(), GetFileInfo(userDataDirectory, userDataListFilename));
                    }
                    else
                    {
                        File.Delete(GetFileInfo(userDataDirectory, currentKeyID.ToString() + "." + userDataFileExtension).FullName);
                        userDataFilesList.Remove(currentKeyID.ToString() + "." + userDataFileExtension);
                        WriteStringsToFile(userDataFilesList.ToArray(), GetFileInfo(userDataDirectory, userDataListFilename));
                    }
                }
                else
                {
                    File.Delete(GetFileInfo(userDataDirectory, currentKeyID.ToString() + "." + userDataFileExtension).FullName);
                    userDataFilesList.Remove(currentKeyID.ToString() + "." + userDataFileExtension);
                    WriteStringsToFile(userDataFilesList.ToArray(), GetFileInfo(userDataDirectory, userDataListFilename));
                }
            }
            else
                Debug.LogWarning("UserKeyID not valid. You probably have not set a user key.");
        }
            

        public static Dictionary<UserDataKey, string> LoadUserData(KeyID keyIDToLoad)
        {
            if (keyIDToLoad != null)
            {
                List<string> strings = ReadStringsFromFile(GetFileInfo(userDataDirectory, keyIDToLoad.ToString() + "." + userDataFileExtension));
                Dictionary<UserDataKey, string> userData = new Dictionary<UserDataKey, string>();
                foreach (string str in strings)
                {
                    string[] separators = new string[1];
                    separators[0] = ",";
                    string[] keyAndValue = str.Split(separators, 2, System.StringSplitOptions.None);
                    userData.Add(keyAndValue[0], keyAndValue[1]);
                }
                return userData;
            }
            return null;
        }

        private static bool HandleUserDataWWWResponse(ref WWW userDatawww,
            ref bool userDatawwwBusy,
            ref KeyID userDatawwwKeyID,
            ref Dictionary<UserDataKey, string> userData,
            KeyID wwwKeyID,
            KeyID currentKeyID,
            List<string> userDataFilesList)
        {
            if (userDatawww != null)
            {
                if (userDatawwwBusy)
                {
                    if ((userDatawww.isDone) && (!string.IsNullOrEmpty(userDatawww.error)))
                    {
                        Debug.LogWarning("Send User Data Error: " + userDatawww.error);
                        userDatawwwBusy = false;
                        ConnectionLogger.Instance.HTTPError();
                    }
                    else if (userDatawww.isDone)
                    {
                        if (!string.IsNullOrEmpty(userDatawww.text.Trim()))
                        {
                            Debug.LogWarning("Response from server: " + userDatawww.text);
                        }
                        if (wwwKeyID == currentKeyID)
                            userData.Clear();
                        else
                        {
                            File.Delete(GetFileInfo(userDataDirectory, wwwKeyID.ToString() + "." + userDataFileExtension).FullName);
                            userDataFilesList.Remove(wwwKeyID.ToString() + "." + userDataFileExtension);
                            WriteStringsToFile(userDataFilesList.ToArray(), GetFileInfo(userDataDirectory, userDataListFilename));
                        }

                        userDatawwwBusy = false;
                        userDatawwwKeyID = null;
                        ConnectionLogger.Instance.HTTPSuccess();
                    }
                }
            }

            return true;
        }

        public void SendAllBuffered()
        {
            SendFrame();

            if (offBufferFull)
                offBufferFull = !SendBuffer(RemoveTrailingNulls(OffBuffer));

            SendBuffer(GetDataInActiveBuffer());
            bufferPos = 0;
        }

        public void Restart()
        {
            SendFrame();
            //SendStreamValue(TelemetryTools.Stream.FrameTime, System.DateTime.UtcNow.Ticks);
            startTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
            SendKeyValuePair(Event.TelemetryStart, System.DateTime.UtcNow.ToString("u"));
            SendEvent(Event.TelemetryStart);
        }

        private bool SendBuffer(byte[] data, bool httpOnly = false)
        {
#if POSTENABLED
            if (httpPostEnabled)
            {
                SaveDataOnWWWErrorIfWeCan();

                if (!wwwBusy)
                {
                    if (keyManager.HasKey)
                    {
                        SendByHTTPPost(data, sessionID, sequenceID, fileExtension, keyManager.CurrentKey, keyManager.CurrentKeyID, uploadURL, ref www, out wwwData, out wwwSequenceID, out wwwSessionID, out wwwBusy, out wwwKey, out wwwKeyID);
                        ConnectionLogger.Instance.AddDataSentByHTTPSinceUpdate((uint)data.Length);
                        sequenceID++;
                        return true;
                    }
                }
                else
                    Debug.LogWarning("Cannot send buffer: WWW object busy");
            }
#endif
#if LOCALSAVEENABLED
            if (!httpOnly)
            {
                if (WriteCacheFile(data, sessionID, sequenceID, keyManager.CurrentKeyID))
                {
                    sequenceID++;
                    return true;
                }
            }
            if (!httpOnly)
#endif
                Debug.LogWarning("Could not deal with buffer: " + BytesToString(data));

            return false;
        }

        private void BufferData(byte[] data, bool newFrame = false)
        {
            if (keyManager.UsingKey)
            {
                ConnectionLogger.Instance.AddDataLoggedSinceUpdate((uint)data.Length);

                if (newFrame)
                {
                    if (frameID != 0)
                    {
                        byte[] endFrame = StringToBytes("}\n"); // newline required for mongo import?
                        System.Buffer.BlockCopy(endFrame, 0, frameBuffer, frameBufferPos, endFrame.Length);
                        frameBufferPos += endFrame.Length;
                    }

                    if (frameBufferPos + bufferPos > bufferSize)
                    {
                        if (offBufferFull)
                        {
                            Debug.LogWarning("Overflow local telemetry buffer, data overwritten");
                            ConnectionLogger.Instance.AddLostData((uint)RemoveTrailingNulls(OffBuffer).Length);
                        }

                        Array.Clear(ActiveBuffer, bufferPos, outboxBuffer1.Length - bufferPos);

                        buffer1Active = !buffer1Active;
                        offBufferFull = true;
                        bufferPos = 0;
                    }

                    System.Buffer.BlockCopy(frameBuffer, 0, ActiveBuffer, bufferPos, frameBufferPos);
                    bufferPos += frameBufferPos;
                    frameBufferPos = 0;

                    frameID++;
                }

                if (frameBufferPos + data.Length < frameBuffer.Length)
                {
                    System.Buffer.BlockCopy(data, 0, frameBuffer, frameBufferPos, data.Length);
                    frameBufferPos += data.Length;
                }
                else
                {
                    Debug.LogWarning("Overflow frame buffer, data lost");
                    ConnectionLogger.Instance.AddLostData((uint)data.Length);
                }
            }
            else
                Debug.LogWarning("Cannot buffer data without it being associated with a unique key. Create a new key.");
        }

        private long GetTimeFromStart()
        {
            return (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) -startTime;
        }

        private byte[] GetDataInActiveBuffer()
        {
            byte[] partBuffer = new byte[bufferPos];
            if (bufferPos > 0)
                System.Buffer.BlockCopy(ActiveBuffer, 0, partBuffer, 0, partBuffer.Length);
            return partBuffer;
        }

        public void SendFrame()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("{\"id\":");
            sb.Append(frameID);
            sb.Append("");
            BufferData(StringToBytes(sb.ToString()), newFrame: true);
        }

        public void SendEvent(string name, Milliseconds time)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(GetTimeFromStart().ToString());
            sb.Append("\":\"");
            sb.Append(name);
            sb.Append("\"");
            BufferData(StringToBytes(sb.ToString()));
        }

        public void SendEvent(string name)
        {
            SendEvent(name, GetTimeFromStart());
        }

        public void SendKeyValuePair(string key, string value)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":\"");
            sb.Append(value);
            sb.Append("\"");
            BufferData(StringToBytes(sb.ToString()));
        }

        public void SendValue(string key, ValueType value, Milliseconds time)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":{\"v\":");
            sb.Append(value.ToString());
            sb.Append(",\"t\":");
            sb.Append(GetTimeFromStart().ToString());
            sb.Append("}");
            BufferData(StringToBytes(sb.ToString()));
        }

        public void SendValue(string key, ValueType value)
        {
            SendValue(key, value, GetTimeFromStart());
        }

        public void SendStreamString(string key, string value)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":\"");
            sb.Append(value);
            sb.Append("\"");
            BufferData(StringToBytes(sb.ToString()));
        }


        public void SendStreamValue(string key, ValueType value)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":");
            sb.Append(value.ToString());
            BufferData(StringToBytes(sb.ToString()));
        }

        public void SendStreamValue(string key, bool value)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":");
            if (value)
                sb.Append("true");
            else
                sb.Append("false");
            BufferData(StringToBytes(sb.ToString()));
        }

        public void SendStreamValueBlock(string key, byte[] values)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString());
                if (i < values.Length - 1)
                    sb.Append(",");
            }
            sb.Append("]");

            BufferData(StringToBytes(sb.ToString()));
        }

        public void SendStreamValueBlock(string key, short[] values)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString());
                if (i < values.Length - 1)
                    sb.Append(",");
            }
            sb.Append("]");

            BufferData(StringToBytes(sb.ToString()));
        }

        public void SendStreamValueBlock(string key, int[] values)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString());
                if (i < values.Length - 1)
                    sb.Append(",");
            }
            sb.Append("]");

            BufferData(StringToBytes(sb.ToString()));
        }

        public static void SendStreamValueBlock(string key, long[] values)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString());
                if (i<values.Length-1)
                    sb.Append(",");
            }
            sb.Append("]");

            Instance.BufferData(StringToBytes(sb.ToString()));
        }

        public void SendStreamValueBlock(string key, float[] values)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString());
                if (i < values.Length - 1)
                    sb.Append(",");
            }
            sb.Append("]");

            BufferData(StringToBytes(sb.ToString()));
        }

        public void SendStreamValueBlock(string key, double[] values)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":[");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString());
                if (i < values.Length - 1)
                    sb.Append(",");
            }
            sb.Append("]");

            BufferData(StringToBytes(sb.ToString()));
        }

        public void SendByteDataBase64(string key, byte[] data)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":\"");
            sb.Append(data.Length);
            sb.Append(",");
            sb.Append(System.Convert.ToBase64String(data));
            sb.Append("\"");

            BufferData(StringToBytes(sb.ToString()));
        }

        //Looking at the Mongo spec, it seems that it doesn't support this sort of encoding, use SendByteDataBase64 instead.
        public void SendByteDataBinary(string key, byte[] data)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(",\"");
            sb.Append(key);
            sb.Append("\":\"");
            byte[] b1 = StringToBytes(sb.ToString());

            byte[] b2 = BitConverter.GetBytes((System.Int32) data.Length);
            byte nullByte = (byte) 0;
            byte[] b3 = StringToBytes("\"");

            byte[] output = new byte[data.Length + b1.Length + b2.Length + 1 + b3.Length];
            System.Buffer.BlockCopy(b1,0,output,0,b1.Length);
            System.Buffer.BlockCopy(b2,0,output,b1.Length,b2.Length);
            output[b1.Length] = nullByte;
            System.Buffer.BlockCopy(data,0,output,b1.Length+b2.Length+1,data.Length);
            System.Buffer.BlockCopy(b3,0,output,b1.Length+b2.Length+data.Length+1,b3.Length);

            BufferData(output);
        }

#if POSTENABLED
        private static void SendByHTTPPost( byte[] data,
                                            SessionID sessionID,
                                            SequenceID sequenceID,
                                            FilePath fileExtension,
                                            UniqueKey uniqueKey,
                                            KeyID uniqueKeyID,
                                            URL uploadURL,
                                            ref WWW www,
                                            out byte[] wwwData,
                                            out SequenceID wwwSequenceID,
                                            out SessionID wwwSessionID,
                                            out bool wwwBusy,
                                            out UniqueKey wwwKey,
                                            out KeyID wwwKeyID)
        {

            if (!String.IsNullOrEmpty(uniqueKey))
            {
                WWWForm form = new WWWForm();
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append(sessionID);
                sb.Append(".");
                sb.Append(sequenceID);
                sb.Append(".");
                sb.Append(fileExtension);
                form.AddField("key", uniqueKey);
                form.AddField("session", sessionID.ToString());
                form.AddBinaryData(fileExtension, data, sb.ToString());

                www = new WWW(uploadURL, form);
                wwwBusy = true;

                wwwData = new byte[data.Length];
                System.Buffer.BlockCopy(data, 0, wwwData, 0, data.Length);
                wwwSequenceID = sequenceID;
                wwwSessionID = sessionID;
                wwwKey = uniqueKey;
                wwwKeyID = uniqueKeyID;
                ConnectionLogger.Instance.HTTPRequestSent();
            }
            else
            {
                Debug.LogWarning("Cannot send data without a key to the server");
                wwwBusy = false;
                wwwData = new byte[0];
                wwwSessionID = null;
                wwwSequenceID = null;
                wwwKey = null;
                wwwKeyID = null;
            }
                
        }

        private static bool HandleWWWErrors(ref WWW www, ref byte[] wwwData, ref SessionID wwwSessionID, ref SequenceID wwwSequenceID, ref bool wwwBusy, ref UniqueKey wwwKey, ref KeyID wwwKeyID)
        {
            if (www != null)
            {
                if ((www.isDone) && (!string.IsNullOrEmpty(www.error)))
                {
                    Debug.LogWarning("Send Data Error: " + www.error);
                    ConnectionLogger.Instance.HTTPError();
                    return false;
                }
                else if (www.isDone)
				{
					if (!string.IsNullOrEmpty(www.text.Trim()))
					{
						Debug.LogWarning ("Response from server: " + www.text);
					}
                    DisposeWWW(ref www, ref wwwData, ref wwwSessionID, ref wwwSequenceID, ref wwwBusy, ref wwwKey, ref wwwKeyID);
				}
            }
            ConnectionLogger.Instance.HTTPSuccess();
            return true;
        }

        private static void DisposeWWW(ref WWW www, ref byte[] wwwData, ref SessionID wwwSessionID, ref SequenceID wwwSequenceID, ref bool wwwBusy, ref UniqueKey wwwKey, ref KeyID wwwKeyID)
        {
            wwwBusy = false;
            wwwData = new byte[0];
            wwwSessionID = null;
            wwwSequenceID = null;
            wwwKey = null;
            wwwKeyID = null;
        }
#endif

#if LOCALSAVEENABLED
        private static void WriteDataToFile(byte[] data, FileInfo file)
        {
            FileStream fileStream = null;
            try
            {
                fileStream = file.Open(FileMode.Create);
                fileStream.Write(data, 0, data.Length);
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                    fileStream = null;
                }
            }
        }

        private static void ParseCacheFileName(FilePath directory, FilePath filename, out SessionID sessionID, out SequenceID sequenceID, out KeyID keyID)
        {
            sessionID = null;
            sequenceID = null;
            keyID = null;
            directory = LocalFilePath(directory);
            if (Directory.Exists(directory))
            {
                uint sqID = 0;
                uint snID = 0;
                uint kID = 0;
                string[] separators = new string[1];
                separators[0] = ".";
                string[] fileDetails = filename.Split(separators, 4, System.StringSplitOptions.None);
                bool parsed = UInt32.TryParse(fileDetails[0], out snID) && UInt32.TryParse(fileDetails[1], out sqID) && UInt32.TryParse(fileDetails[2], out kID);

                if (parsed)
                {
                    sessionID = snID;
                    sequenceID = sqID;
                    keyID = kID;
                }
                else
                    Debug.LogWarning("Failed to parse filename. List of cache files may be corrputed.");
            }
        }

        private static bool LoadFromCacheFile(FilePath directory, FilePath filename, out byte[] data)
        {
            data = new byte[0];

            FilePath cacheFile = directory + "/" + filename;
            if (File.Exists(cacheFile))
            {
                data = File.ReadAllBytes(cacheFile);
                return true;
            }
            else
            {
                Debug.LogWarning("Attempted to load from from non-existant cache file: " + cacheFile);
            }
            return false;
        }


        private static void WriteStringsToFile(string[] stringList, FileInfo file)
        {
            FileStream fileStream = null;
            byte[] newLine = StringToBytes("\n");
            try
            {
                fileStream = file.Open(FileMode.Create);

                foreach (FilePath str in stringList)
                {
                    byte[] bytes = StringToBytes(str);
                    fileStream.Write(bytes, 0, bytes.Length);
                    fileStream.Write(newLine, 0, newLine.Length);
                }
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                    fileStream = null;
                }
            }
        }


        private static List<FilePath> ReadStringsFromFile(FileInfo file)
        {
            List<FilePath> list = new List<FilePath>();

            if (file.Exists)
            {
                FileStream fileStream = null;
                try
                {
                    fileStream = file.Open(FileMode.Open);

                    byte[] bytes = new byte[fileStream.Length];
                    fileStream.Read(bytes, 0, (int)fileStream.Length);
                    string s = BytesToString(bytes);
                    string[] separators = new string[1];
                    separators[0] = "\n";
                    string[] lines = s.Split(separators, Int32.MaxValue, StringSplitOptions.RemoveEmptyEntries);
                    list = new List<FilePath>(lines);
                    return list;
                }
                catch (IOException)
                {
                    return list;
                }
                finally
                {
                    if (fileStream != null)
                    {
                        fileStream.Close();
                        fileStream = null;
                    }
                }
            }
            Debug.LogWarning("Attempted to read strings from non-existant file: " + file.FullName);
            return list;
        }

        private static T ReadValueFromFile<T>(FileInfo file)
        {
            if (file.Exists)
            {

                FileStream fileStream = null;
                try
                {
                    fileStream = file.Open(FileMode.Open);

                    byte[] bytes = new byte[fileStream.Length];
                    fileStream.Read(bytes, 0, (int)fileStream.Length);
                    string s = BytesToString(bytes);

                    return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(s);
                }
                catch
                {
                    return default(T);
                }
                finally
                {
                    if (fileStream != null)
                    {
                        fileStream.Close();
                        fileStream = null;
                    }
                }
            }

            Debug.LogWarning("Attempted to read value from non-existant file: " + file.FullName);

            return default(T);
        }

        private static void WriteValueToFile(ValueType value, FileInfo file)
        {
            FileStream fileStream = null;
            try
            {
                fileStream = file.Open(FileMode.Create);

                byte[] bytes = StringToBytes(value.ToString() + "\n");
                fileStream.Write(bytes, 0, bytes.Length);
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                    fileStream = null;
                }
            }
        }

        private static bool IsFileOpen(FileInfo file)
        {
            if (file != null)
            {
                FileStream stream = null;

                try
                {
                    stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException)
                {
                    return true;
                }
                finally
                {
                    if (stream != null)
                        stream.Close();
                }
            }
            return false;
        }

        private static FileInfo GetFileInfo(FilePath directory, FilePath filename)
        {
            directory = LocalFilePath(directory);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Debug.Log("Created directory " + directory);
            }

            FilePath filePath = LocalFilePath(directory + "/" + filename);
            return new FileInfo(filePath);
        }

        private static FileInfo GetFileInfo(FilePath directory,
                                            SessionID sessionID,
                                            SequenceID sequenceID,
                                            KeyID keyID,
                                            long time,
                                            FilePath fileExtension)
        {
            directory = LocalFilePath(directory);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Debug.Log("Created directory " + directory);
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(sessionID);
            sb.Append(".");
            sb.Append(sequenceID);
            sb.Append(".");
            sb.Append(keyID);
            sb.Append(".");
            sb.Append(time);
            sb.Append(".");
            sb.Append(fileExtension);

            FilePath filePath = directory + "/" +sb.ToString();
            return new FileInfo(filePath);
        }

        private static FilePath LocalFilePath(FilePath filename)
        {
           /* if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                FilePath path = Application.dataPath.Substring(0, Application.dataPath.Length - 5);
                path = path.Substring(0, path.LastIndexOf('/'));
                return Path.Combine(Path.Combine(path, "Documents"), filename);
            }
            else if (Application.platform == RuntimePlatform.Android)
            {
                FilePath path = Application.persistentDataPath;
                path = path.Substring(0, path.LastIndexOf('/'));
                return Path.Combine(path, filename);
            }
            else
            {*/
                //TODO: check what's going on here.
                FilePath path = Application.persistentDataPath + "/";
                //path = path.Substring(0, path.LastIndexOf('/')+1);
                //return path + filename;
                return Path.Combine(path, filename);
            //}
        }

        private bool WriteCacheFile(byte[] data, SessionID sessionID, SequenceID sequenceID, KeyID key)
        {
            if (data.Length > 0)
            {
                FileInfo file = GetFileInfo(cacheDirectory, sessionID, sequenceID, key, GetTimeFromStart(), fileExtension);
                if ((!File.Exists(file.FullName)) || (!IsFileOpen(file)))
                {
                    WriteDataToFile(data, file);
                    ConnectionLogger.Instance.AddDataSavedToFileSinceUpdate((uint)data.Length);

                    cachedFilesList.Add(file.Name);
                    //TODO: Append rather than rewrite everything
                    WriteStringsToFile(cachedFilesList.ToArray(), GetFileInfo(cacheDirectory, cacheListFilename));
                    return true;
                }
                else
                {
                    Debug.LogWarning("Couldn't write cache file becasue it was open or it already exists");
                    return false;
                }
            }

            return true;
        }

#endif

#if POSTENABLED
        private void SaveDataOnWWWErrorIfWeCan()
        {
            if (www != null)
            {
                if (wwwBusy)
                {
                    if (!HandleWWWErrors(ref www, ref wwwData, ref wwwSessionID, ref wwwSequenceID, ref wwwBusy, ref wwwKey, ref wwwKeyID))
                    {
#if LOCALSAVEENABLED
                        WriteCacheFile(wwwData, wwwSessionID, wwwSequenceID, wwwKeyID);
                        DisposeWWW(ref www, ref wwwData, ref wwwSessionID, ref wwwSequenceID, ref wwwBusy, ref wwwKey, ref wwwKeyID);
                        
#else
                        ConnectionLogger.Instance.AddLostData(wwwData.Length);
                        DisposeWWW(ref www, ref wwwData, ref wwwSessionID, ref wwwSequenceID, ref wwwBusy, ref wwwKey, ref wwwKeyID);
#endif
                    }
                }
            }
            
        }
#endif

        private static byte[] StringToBytes(string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }

        private static string BytesToString(byte[] bytes)
        {
            return Encoding.ASCII.GetString(bytes);
        }

        private static byte[] RemoveTrailingNulls(byte[] data)
        {
            int i=data.Length-1;
            while (data[i] == 0)
                i--;
            byte[] trimmed = new byte[i+2];
            System.Buffer.BlockCopy(data,0,trimmed,0,trimmed.Length);
            return trimmed;
        }
    }
}
