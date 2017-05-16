namespace Sitecore.Support.Hooks
{
  using Sitecore.Configuration;
  using Sitecore.Diagnostics;
  using Sitecore.Events.Hooks;
  using Sitecore.SecurityModel;
  public class UpdateMerchandisingManagerToCallFixedAPI : IHook
  {
    public void Initialize()
    {
      using (new SecurityDisabler())
      {
        var databaseName = "core";
        string itemPath1 = "/sitecore/client/Applications/MerchandisingManager/VariantDetail/PageSettings/Tabs/Details";
        string itemPath2 = "/sitecore/client/Applications/MerchandisingManager/ProductDetail/PageSettings/ProductTabs/Details";

        var fieldName = "__Renderings";

        // protects from refactoring-related mistakes
        var type = typeof(Sitecore.Support.Commerce.UX.Merchandising.Code.FixedCommercePricingController);

        var typeName = type.FullName;
        var assemblyName = type.Assembly.GetName().Name;
        var fieldValue = $"sitecore%2fshell%2fcommerce%2fmerchandising%2fFixedCommercePricing%2fGetPricingForProduct";
        var oldFieldValue = $"sitecore%2fshell%2fcommerce%2fmerchandising%2fCommercePricing%2fGetPricingForProduct";

        var database = Factory.GetDatabase(databaseName);

        var item1 = database.GetItem(itemPath1);
        var item2 = database.GetItem(itemPath2);

        if (item1[fieldName].Contains(fieldValue) && item2[fieldName].Contains(fieldValue))
        {
          // already installed
          return;
        }

        Log.Info($"Installing {assemblyName}", this);

        item1.Editing.BeginEdit();
        item1[fieldName]=item1[fieldName].Replace(oldFieldValue, fieldValue);
        item1.Editing.EndEdit();

        item2.Editing.BeginEdit();
        item2[fieldName]=item2[fieldName].Replace(oldFieldValue, fieldValue);
        item2.Editing.EndEdit();
      }

    }
  }
}
