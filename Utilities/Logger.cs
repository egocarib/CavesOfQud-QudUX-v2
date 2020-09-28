using System.Collections.Generic;

namespace QudUX.Utilities
{
    public static class Logger
    {
        private static HashSet<string> UniqueMessages = new HashSet<string>();
        private static Dictionary<string, CompiledLog> CompiledMessages = new Dictionary<string, CompiledLog>();

        public class CompiledLog
        {
            public string PrefaceMessage = string.Empty;
            public List<string> LogLines = new List<string>();
        }

        public static void Log(string message) => UnityEngine.Debug.Log($"QudUX: {message}");

        public static bool LogUnique(string message)
        {
            if (UniqueMessages.Contains(message))
            {
                return false;
            }
            UniqueMessages.Add(message);
            Log(message);
            return true;
        }

        public static void LogCompiled(string type, string message, string prefaceMessage = "")
        {
            CompiledLog cLog;
            if (CompiledMessages.ContainsKey(type))
            {
                cLog = CompiledMessages[type];
                cLog.PrefaceMessage = string.IsNullOrEmpty(prefaceMessage) ? cLog.PrefaceMessage : prefaceMessage;
            }
            else
            {
                cLog = new CompiledLog();
                cLog.PrefaceMessage = prefaceMessage;
                CompiledMessages[type] = cLog;
            }
            cLog.LogLines.Add(message);
        }

        public static void FlushCompiledLog(string type)
        {
            CompiledLog cLog;
            if (CompiledMessages.ContainsKey(type))
            {
                cLog = CompiledMessages[type];
                string text = string.IsNullOrEmpty(cLog.PrefaceMessage) ? string.Empty : $"{cLog.PrefaceMessage}";
                while (cLog.LogLines.Count > 0)
                {
                    text += text.Length > 0 ? "\n" : string.Empty;
                    text += $"{cLog.LogLines[0]}";
                    cLog.LogLines.RemoveAt(0);
                }
                Log(text);
            }
        }
    }
}
