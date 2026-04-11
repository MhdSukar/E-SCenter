using System;
using System.Threading.Tasks;
using ESCenter.Core;

namespace ESCenter.Core
{
    internal static class FireAndForgetExtensions
    {
        internal static void FireAndForget(this Task task, string context = "")
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null)
                {
                    var msg = string.IsNullOrWhiteSpace(context)
                        ? t.Exception.GetBaseException().Message
                        : $"[{context}] {t.Exception.GetBaseException().Message}";
                    AppLogger.Error(msg);
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
