using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace TD.Config
{
    /// <summary>
    /// 从 StreamingAssets/TD 读取 JSON 的最小实现。
    /// PC/Editor 走 File IO；Android 走 UnityWebRequest。
    /// </summary>
    public class StreamingAssetsJsonLoader : IJsonLoader
    {
        public async Task<T> LoadAsync<T>(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) throw new ArgumentException("relativePath is null or empty");
            string root = Path.Combine(Application.streamingAssetsPath, "TD");
            string fullPath = Path.Combine(root, relativePath).Replace("\\", "/");

            string json;
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var req = UnityWebRequest.Get(fullPath))
            {
                await AwaitRequest(req);
                if (req.result != UnityWebRequest.Result.Success)
                    throw new IOException($"Load failed: {fullPath}, error: {req.error}");
                json = req.downloadHandler.text;
            }
#else
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"JSON not found: {fullPath}");
            json = await Task.Run(() => File.ReadAllText(fullPath));
#endif
            var data = JsonUtility.FromJson<T>(json);
            if (data == null)
                throw new Exception($"JSON parse failed for {relativePath}");
            return data;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static Task AwaitRequest(UnityWebRequest req)
        {
            var tcs = new TaskCompletionSource<bool>();
            var op = req.SendWebRequest();
            op.completed += _ => tcs.TrySetResult(true);
            return tcs.Task;
        }
#endif
    }
}
