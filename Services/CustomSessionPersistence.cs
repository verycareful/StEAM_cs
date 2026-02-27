using Newtonsoft.Json;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace StEAM_.NET_main.Services;

public class CustomSessionPersistence : IGotrueSessionPersistence<Session>
{
    private const string SessionKey = "supabase_session_json";

    public void DestroySession()
    {
        try
        {
            Preferences.Remove(SessionKey);
            System.Diagnostics.Debug.WriteLine("[SessionPersistence] Session destroyed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SessionPersistence] DestroySession error: {ex.Message}");
        }
    }

    public Session? LoadSession()
    {
        try
        {
            var json = Preferences.Get(SessionKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                System.Diagnostics.Debug.WriteLine("[SessionPersistence] No saved session found");
                return null;
            }

            var session = JsonConvert.DeserializeObject<Session>(json);
            System.Diagnostics.Debug.WriteLine($"[SessionPersistence] Session loaded, has refresh token: {!string.IsNullOrEmpty(session?.RefreshToken)}");
            return session;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SessionPersistence] LoadSession error: {ex.Message}");
            DestroySession();
            return null;
        }
    }

    public void SaveSession(Session session)
    {
        try
        {
            var json = JsonConvert.SerializeObject(session);
            Preferences.Set(SessionKey, json);
            System.Diagnostics.Debug.WriteLine($"[SessionPersistence] Session saved ({json.Length} chars)");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SessionPersistence] SaveSession error: {ex.Message}");
        }
    }
}
