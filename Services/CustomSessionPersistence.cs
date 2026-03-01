using Newtonsoft.Json;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

#if ANDROID
using Android.Util;
#endif

namespace StEAM_.NET_main.Services;

public class CustomSessionPersistence : IGotrueSessionPersistence<Session>
{
    private const string SessionKey = "supabase_session_json";
    private const string LogTag = "StEAM.Session";

    public void SaveSession(Session session)
    {
        try
        {
            var json = JsonConvert.SerializeObject(session);
            SecureStorage.Default.SetAsync(SessionKey, json).GetAwaiter().GetResult();
#if ANDROID
            Log.Info(LogTag, $"Session saved to SecureStorage ({json.Length} chars)");
#endif
        }
        catch (Exception ex)
        {
#if ANDROID
            Log.Error(LogTag, $"SaveSession failed: {ex.GetType().Name}: {ex.Message}");
#endif
        }
    }

    public Session? LoadSession()
    {
#if ANDROID
        Log.Info(LogTag, "LoadSession() called");
#endif
        try
        {
            var json = SecureStorage.Default.GetAsync(SessionKey).GetAwaiter().GetResult();
#if ANDROID
            Log.Info(LogTag, $"SecureStorage returned: {(string.IsNullOrEmpty(json) ? "null/empty" : json.Length + " chars")}");
#endif
            if (string.IsNullOrEmpty(json))
            {
#if ANDROID
                Log.Info(LogTag, "No saved session found in SecureStorage");
#endif
                return null;
            }

            var session = JsonConvert.DeserializeObject<Session>(json);
#if ANDROID
                Log.Info(LogTag, $"Session loaded, has refresh token: {!string.IsNullOrEmpty(session?.RefreshToken)}");
#endif
            return session;
        }
        catch (Exception ex)
        {
            // Keystore invalidation (new screen lock, backup restore) throws here — clean up and force re-login
#if ANDROID
                Log.Error(LogTag, $"LoadSession failed: {ex.GetType().Name}: {ex.Message}");
#endif
            DestroySession();
            return null;
        }
    }

    public void DestroySession()
    {
        try
        {
            SecureStorage.Default.Remove(SessionKey);
#if ANDROID
            Log.Info(LogTag, "Session destroyed");
#endif
        }
        catch (Exception ex)
        {
#if ANDROID
                Log.Error(LogTag, $"DestroySession failed: {ex.GetType().Name}: {ex.Message}");
#endif
        }
    }
}
