using MediaBrowser.Model.Entities;

namespace KinopoiskWhite.Extensions;

using Common;
using Providers;


public static class ItemLookupInfoExtensions
{
    public class Error(string message) : Base.Error(message)
    {
        public class WrongValue() : Base.Error("Wrong value");
    }
    private static readonly string DefaultId = Constants.ProviderId;

    public static bool HasDefaultId(this IHasProviderIds info)
    => info.TryGetDefaultId(out var _);

    public static int GetDefaultId(this IHasProviderIds info)
    {
        try
        {
            var value = info.GetProviderId(DefaultId);
            return System.Convert.ToInt32(value ?? throw new Error.WrongValue());
        }
        catch
        {
            throw new Error.WrongValue();
        }
    }

    public static bool TryGetDefaultId(this IHasProviderIds info, out int kid)
    {
        try
        {
            kid = info.GetDefaultId();
            return true;
        }
        catch
        {
            kid = -1;
            return false;
        }
    }

    public static void SetDefaultId(this IHasProviderIds info, int kid)
    => info.SetProviderId(DefaultId, System.Convert.ToString(kid));


    private static readonly string ContentId = Constants.ProviderName;

    public static bool HasContentId(this IHasProviderIds info)
    => info.HasProviderId(ContentId);

    public static string GetContentId(this IHasProviderIds info)
    => info.GetProviderId(ContentId);

    public static bool TryGetContentId(this IHasProviderIds info, out string cid)
    => info.TryGetProviderId(ContentId, out cid);

    public static void SetContentId(this IHasProviderIds info, string cid)
    {
        if (cid != null) info.SetProviderId(ContentId, cid);
    }
}