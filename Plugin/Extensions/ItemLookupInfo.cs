using MediaBrowser.Model.Entities;

namespace Plugin.Extensions;

using Common;


public static class ItemLookupInfoExtensions
{
    private static readonly string DefaultId = Constants.ProviderId;

    public static bool HasDefaultId(this IHasProviderIds info)
    => info.HasProviderId(DefaultId);

    public static string GetDefaultId(this IHasProviderIds info)
    => info.GetProviderId(DefaultId);

    public static bool TryGetDefaultId(this IHasProviderIds info, out string kid)
    => info.TryGetProviderId(DefaultId, out kid);

    public static void SetDefaultId(this IHasProviderIds info, string kid)
    => info.SetProviderId(DefaultId, kid);

    private static readonly string ContentId = Constants.ProviderName;

    public static bool HasContentId(this IHasProviderIds info)
    => info.HasProviderId(ContentId);

    public static string GetContentId(this IHasProviderIds info)
    => info.GetProviderId(ContentId);

    public static bool TryGetContentId(this IHasProviderIds info, out string cid)
    => info.TryGetProviderId(ContentId, out cid);

    public static void SetContentId(this IHasProviderIds info, string cid)
    => info.SetProviderId(ContentId, cid);
}