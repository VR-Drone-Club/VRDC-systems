#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace UdonSharpEditor
{
    [InitializeOnLoad]
    public static class DronePositionLogReader
    {
        static DronePositionLogReader()
        {
            DronePositionLogReader.InitLogWatcher();
        }
        private class LogFileState
        {
            public string playerName;
            public long lineOffset = -1;
            public string nameColor = "0000ff";
        }

        private static Queue<string> _debugOutputQueue = new Queue<string>();

        // Log watcher vars
        private static FileSystemWatcher _logDirectoryWatcher;
        private static object _logModifiedLock = new object();
        private static Dictionary<string, LogFileState> _logFileStates = new Dictionary<string, LogFileState>();
        private static HashSet<string> _modifiedLogPaths = new HashSet<string>();

        public static void InitLogWatcher()
        {
            Debug.Log("MapReportLogWatcher subscribed to updates and logs");
            EditorApplication.update += OnEditorUpdate;
            //Application.logMessageReceived += OnLog;
        }

        private static bool ShouldListenForVRC()
        {
            return true;
        }        
        
        private static bool InitializeScriptLookup()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return false;
            
            if (_logDirectoryWatcher == null && ShouldListenForVRC())
            {
                AssemblyReloadEvents.beforeAssemblyReload += CleanupLogWatcher;

                // Now setup the filesystem watcher
                string[] splitPath = Application.persistentDataPath.Split('/', '\\');
                string VRCDataPath = string.Join("\\", splitPath.Take(splitPath.Length - 2)) + "\\VRChat\\VRChat";

                if (Directory.Exists(VRCDataPath))
                {
                    _logDirectoryWatcher = new FileSystemWatcher(VRCDataPath, "output_log_*.txt");
                    _logDirectoryWatcher.IncludeSubdirectories = false;
                    _logDirectoryWatcher.NotifyFilter = NotifyFilters.LastWrite;
                    _logDirectoryWatcher.Changed += OnLogFileChanged;
                    _logDirectoryWatcher.InternalBufferSize = 1024;
                    _logDirectoryWatcher.EnableRaisingEvents = false;
                    Debug.Log("Set up log watcher");
                }
                else
                {
                    if (!_didMissingDataError)
                    {
                        Debug.LogError("Could not locate VRChat data directory for exception watcher, make sure you have VRChat installed and have run it at least once or turn off exception watching in the UdonSharp project settings.");
                        _didMissingDataError = true;
                    }

                    return false;
                }
            }

            return true;
        }


        private static bool _didMissingDataError;


        private static void CleanupLogWatcher()
        {
            if (_logDirectoryWatcher != null)
            {
                _logDirectoryWatcher.EnableRaisingEvents = false;
                _logDirectoryWatcher.Changed -= OnLogFileChanged;
                _logDirectoryWatcher.Dispose();
                _logDirectoryWatcher = null;
            }

            EditorApplication.update -= OnEditorUpdate;
            Application.logMessageReceived -= OnLog;
            AssemblyReloadEvents.beforeAssemblyReload -= CleanupLogWatcher;
        }

        private static void OnLogFileChanged(object source, FileSystemEventArgs args)
        {
            lock (_logModifiedLock)
            {
                _modifiedLogPaths.Add(args.FullPath);
            }
        }

        private static void OnLog(string logStr, string stackTrace, LogType type)
        {
            if (logStr.Contains("DroneTracker"))
            {
                _debugOutputQueue.Enqueue(logStr);
            }
        }

        private const string MATCH_STR = "\\n\\n\\r\\n\\d{4}.\\d{2}.\\d{2} \\d{2}:\\d{2}:\\d{2} ";
        private static Regex _lineMatch;

        private static void OnEditorUpdate()
        {
            if (!InitializeScriptLookup())
                return;
            
            while (_debugOutputQueue.Count > 0)
            {
                HandleMapReport(_debugOutputQueue.Dequeue());
            }

            bool shouldListenForVRC = ShouldListenForVRC();

            if (_logDirectoryWatcher != null)
                _logDirectoryWatcher.EnableRaisingEvents = shouldListenForVRC;

            if (!shouldListenForVRC)
                return;

            if (_lineMatch == null)
                _lineMatch = new Regex(MATCH_STR, RegexOptions.Compiled);

            List<(string, string)> modifiedFilesAndContents = null;

            lock (_logModifiedLock)
            {
                if (_modifiedLogPaths.Count > 0)
                {
                    modifiedFilesAndContents = new List<(string, string)>();
                    HashSet<string> newLogPaths = new HashSet<string>();

                    foreach (string logPath in _modifiedLogPaths)
                    {
                        if (!_logFileStates.TryGetValue(logPath, out LogFileState logState))
                            _logFileStates.Add(logPath, new LogFileState());

                        logState = _logFileStates[logPath];

                        newLogPaths.Add(logPath);

                        try
                        {
                            FileInfo fileInfo = new FileInfo(logPath);

                            string newLogContent;

                            using (FileStream stream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                using (StreamReader reader = new StreamReader(stream))
                                {
                                    if (logState.playerName == null) // Search for the player name that this log belongs to
                                    {
                                        string fullFileContents = reader.ReadToEnd();

                                        const string searchStr = "[Behaviour] User Authenticated: ";
                                        int userIdx = fullFileContents.IndexOf(searchStr, StringComparison.Ordinal);
                                        if (userIdx != -1)
                                        {
                                            userIdx += searchStr.Length;

                                            int endIdx = userIdx;

                                            while (fullFileContents[endIdx] != '\r' && fullFileContents[endIdx] != '\n') endIdx++; // Seek to end of name

                                            string username = fullFileContents.Substring(userIdx, endIdx - userIdx);

                                            logState.playerName = username;

                                            // Use the log path as well since Build & Test can have multiple of the same display named users
                                            Random random = new Random((username + logPath).GetHashCode());

                                            Color randomUserColor = Color.HSVToRGB((float)random.NextDouble(), 1.00f, 0.9f);
                                            string colorStr = ColorUtility.ToHtmlStringRGB(randomUserColor);

                                            logState.nameColor = colorStr;
                                        }
                                    }

                                    if (logState.lineOffset == -1)
                                    {
                                        reader.BaseStream.Seek(0, SeekOrigin.End);
                                    }
                                    else
                                    {
                                        reader.BaseStream.Seek(logState.lineOffset - 2 < 0 ? 0 : logState.lineOffset - 2, SeekOrigin.Begin); // Subtract 4 characters to pick up the newlines from the prior line for the log forwarding
                                    }

                                    newLogContent = reader.ReadToEnd();

                                    _logFileStates[logPath].lineOffset = reader.BaseStream.Position;
                                    reader.Close();
                                }

                                stream.Close();
                            }

                            newLogPaths.Remove(logPath);

                            if (newLogContent != "")
                                modifiedFilesAndContents.Add((logPath, newLogContent));
                        }
                        catch (IOException)
                        { }
                    }

                    _modifiedLogPaths = newLogPaths;
                }
            }

            if (modifiedFilesAndContents == null)
                return;
            //Debug.Log("Checking modified file");
            foreach ((string filePath, string contents) in modifiedFilesAndContents)
            {
                HandleMapReport(contents);
            }
        }

        // Common messages that can spam the log and have no use for debugging
        private static readonly string[] _filteredPrefixes = {
            "Received Notification: <Notification from username:",
            "Received Message of type: notification content: {{\"id\":\"",
            "Received Message of type: friend-update received at",
            "Received Message of type: friend-active received at",
            "Received Message of type: friend-online received at",
            "Received Message of type: friend-offline received at",
            "Received Message of type: friend-location received at",
            "[VRCFlowNetworkManager] Sending token from provider vrchat",
            "[Always] uSpeak:",
            "Internal: JobTempAlloc has allocations",
            "To Debug, enable the define: TLA_DEBUG_STACK_LEAK in ThreadsafeLinearAllocator.cpp.",
            "PLAYLIST GET id=",
            "Checking server time received at ",
            "[RoomManager] Room metadata is unchanged, skipping update",
            "Setting Custom Properties for Local Player: avatarEyeHeight",
            "HTTPFormUseage:UrlEncoded",
            // Big catch-alls for random irrelevant VRC stuff
            "[API] ",
            "[Behaviour] ",
        };

        private static void HandleMapReport(string message)
        {
            if (!message.Contains("DroneTracker")) return;
            DronePositionViewer.AddRecords(message);
        }
    }
}
#endif