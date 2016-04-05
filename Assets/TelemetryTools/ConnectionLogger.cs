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
using System;

namespace TelemetryTools
{
    public class ConnectionLogger
    {
        private static ConnectionLogger instance;
        public static ConnectionLogger Instance { get { if (instance == null) instance = new ConnectionLogger(); return instance; } }

        private uint totalHTTPRequestsSent;
        public uint TotalHTTPRequestsSent { get { return totalHTTPRequestsSent; } }
        public void HTTPRequestSent() { totalHTTPRequestsSent++; }
        private uint totalHTTPSuccess;
        public uint TotalHTTPSuccess { get { return totalHTTPSuccess; } }
        public void HTTPSuccess() { totalHTTPSuccess++; }
        private uint totalHTTPErrors;
        public uint TotalHTTPErrors { get { return totalHTTPErrors; } }
        public void HTTPError() { totalHTTPErrors++; }

        // Logging and Transfer Rate
        private Milliseconds lastLoggingUpdate;

        private Megabytes dataLogged;
        public Megabytes DataLogged { get { return dataLogged; } }
        private Bytes dataLoggedSinceUpdate;
        public Bytes DataLoggedSinceUpdate { get { return dataLoggedSinceUpdate; } }
        public void AddDataLoggedSinceUpdate(Bytes data) { dataLoggedSinceUpdate += data; }
        private Bytes dataSavedToFileSinceUpdate;
        public Bytes DataSavedToFileSinceUpdate { get { return dataSavedToFileSinceUpdate; } }
        public void AddDataSavedToFileSinceUpdate(Bytes data) { dataSavedToFileSinceUpdate += data; }
        private Bytes dataSentByHTTPSinceUpdate;
        public Bytes DataSentByHTTPSinceUpdate { get { return dataSentByHTTPSinceUpdate; } }
        public void AddDataSentByHTTPSinceUpdate(Bytes data) { dataSentByHTTPSinceUpdate += data; }

        private BytesPerSecond loggingRate;
        public BytesPerSecond LoggingRate { get { return loggingRate; } }
        private BytesPerSecond httpPostRate;
        public BytesPerSecond HTTPPostRate { get { return httpPostRate; } }
        private BytesPerSecond localFileSaveRate;
        public BytesPerSecond LocalFileSaveRate { get { return localFileSaveRate; } }

        private uint totalKeyServerRequestsSent;
        public uint TotalKeyServerRequestsSent { get { return totalKeyServerRequestsSent; } }
        public void KeyServerRequestSent() { totalKeyServerRequestsSent++; }
        private uint totalKeyServerSuccess;
        public uint TotalKeyServerSuccess { get { return totalKeyServerSuccess; } }
        public void KeyServerSuccess() { totalKeyServerSuccess++; }
        private uint totalKeyServerErrors;
        public uint TotalKeyServerErrors { get { return totalKeyServerErrors; } }
        public void KeyServerError() { totalKeyServerErrors++; }

        private Bytes lostData;
        public Bytes LostData { get { return lostData; } }
        public void AddLostData(Bytes data) { lostData += data; }

        private Milliseconds uploadUserDataDelay;
        public Milliseconds UploadUserDataDelay { get { return uploadUserDataDelay; } set { uploadUserDataDelay = value; } }
        private Milliseconds uploadCacheFilesDelay;
        public Milliseconds UploadCacheFilesDelay { get { return uploadCacheFilesDelay; } set { uploadCacheFilesDelay = value; } }

        private Milliseconds requestKeyDelay;
        public Milliseconds RequestKeyDelay { get { return requestKeyDelay; } set { requestKeyDelay = value; } }

        public void Update()
        {
            Milliseconds elapsedTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - lastLoggingUpdate;
            dataLogged += dataLoggedSinceUpdate;

            if (uploadUserDataDelay > 0)
                uploadUserDataDelay -= elapsedTime;
            if (uploadCacheFilesDelay > 0)
                uploadCacheFilesDelay -= elapsedTime;
            if (requestKeyDelay > 0)
                requestKeyDelay -= elapsedTime;

            BytesPerSecond bytePerSecond = 1000 / Math.Max(elapsedTime, 1);
            loggingRate = bytePerSecond * dataLoggedSinceUpdate;
            httpPostRate = bytePerSecond * dataSentByHTTPSinceUpdate;
            localFileSaveRate = bytePerSecond * dataSavedToFileSinceUpdate;

            lastLoggingUpdate = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
            dataLoggedSinceUpdate = 0;
            dataSavedToFileSinceUpdate = 0;
            dataSentByHTTPSinceUpdate = 0;
        }

        public static string GetPrettyLoggingRate() { return Instance.GetPrettyLoggingRateP(); }

        private string GetPrettyLoggingRateP()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("Log Input: ");
            sb.Append(Math.Round((loggingRate / 1024)));
            sb.Append(" KiB/s");

#if POSTENABLED
           sb.Append("    HTTP: ");
            sb.Append(Mathf.Round((httpPostRate / 1024)));
            sb.Append(" KiB/s");
#endif
#if LOCALSAVEENABLED
            sb.Append("    File: ");
            sb.Append(Mathf.Round((localFileSaveRate / 1024)));
            sb.Append(" KiB/s");
#endif
            sb.Append("    Total: ");
            sb.Append((dataLogged / 1024));
            sb.Append(" KiB");
#if LOCALSAVEENABLED
            sb.Append("    Cached Files: ");
            sb.Append(CachedFiles.ToString());
#endif

            sb.Append("    Lost Data: ");
            sb.Append((lostData / 1024));
            sb.Append(" KiB");

            return sb.ToString();
        }
    }
}
