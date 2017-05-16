namespace Sitecore.Support.Commerce.UX.Merchandising.Code
{
  using Newtonsoft.Json.Linq;
  using Sitecore.Commerce.UX.Merchandising;
  using Sitecore.Commerce.UX.Merchandising.Models;
  using Sitecore.Data;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;
  using System;
  using System.Collections.Generic;
  using System.Diagnostics.CodeAnalysis;
  using System.Globalization;
  using System.Net.Http;
  using System.Net.Http.Headers;
  using System.Text;
  using System.Web.Mvc;

  public class FixedCommercePricingController : BusinessController
  {
    // Fields
    private Item serviceSettings = Database.GetDatabase("core").GetItem(ID.Parse("{533488EF-6789-4942-B360-3260BDB22624}"));

    // Methods
    public static bool ClearCommerceFoundationCache(string environment)
    {
      Assert.IsNotNullOrEmpty(environment, "environment");
      Item item = Context.Database.GetItem(ID.Parse("{91652768-0894-47C2-A78E-7D514A1CD3F5}"));
      HttpClient client = new HttpClient
      {
        BaseAddress = new Uri(item["DataServiceUrl"])
      };
      client.DefaultRequestHeaders.Accept.Clear();
      client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
      client.DefaultRequestHeaders.Add("ShopName", item["ShopName"]);
      client.DefaultRequestHeaders.Add("Environment", environment);
      Dictionary<string, object> dictionary = new Dictionary<string, object> {
            {
                "cacheStoreName",
                environment
            }
        };
      return ((client.PutAsJsonAsync<IDictionary<string, object>>("ClearCacheStore()", dictionary).Result.Content.ReadAsAsync<Dictionary<string, object>>().Result["ResponseCode"] as string) == "Ok");
    }

    public JsonResult GetPricingForProduct(string catalogName, string searchTerm, string language, string fields)
    {
      if (string.IsNullOrEmpty(searchTerm))
      {
        return base.Json(string.Empty);
      }
      string currencyCode = string.Empty;
      string sellPrice = string.Empty;
      string listPrice = string.Empty;
      string str4 = string.Empty;
      string requestedEnvironment = base.GetRequestedEnvironment();
      string str6 = this.serviceSettings["ShopName"];
      HttpClient client = new HttpClient();
      client.DefaultRequestHeaders.Accept.Clear();
      client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
      client.DefaultRequestHeaders.Add("ShopName", str6);
      client.DefaultRequestHeaders.Add("Language", language);
      client.DefaultRequestHeaders.Add("Environment", requestedEnvironment);
      string str7 = catalogName + "," + searchTerm;
      char[] separator = new char[] { ',' };
      str4 = searchTerm.Split(separator)[1];
      JToken shopCurrencies = this.GetShopCurrencies(client, str6);
      if (shopCurrencies == null)
      {
        client.Dispose();
        return null;
      }
      List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
      using (IEnumerator<JToken> enumerator = ((IEnumerable<JToken>)shopCurrencies).GetEnumerator())
      {
        while (enumerator.MoveNext())
        {
          currencyCode = enumerator.Current["Code"].ToString();
          if (string.IsNullOrEmpty(str4))
          {
            this.GetSellableItemPrice(client, currencyCode, str7, out sellPrice, out listPrice);
          }
          else
          {
            this.GetSellableItemVariantPrice(client, currencyCode, str7, str4, out sellPrice, out listPrice);
          }
          Dictionary<string, object> item = new Dictionary<string, object> {
                    {
                        "itemId",
                        currencyCode
                    },
                    {
                        "Currency",
                        currencyCode
                    },
                    {
                        "ListPrice",
                        listPrice
                    },
                    {
                        "SellPrice",
                        sellPrice
                    }
                };
          list.Add(item);
        }
      }
      QueryResponse data = new QueryResponse(list.ToArray(), list.Count);
      client.Dispose();
      return base.Json(data);
    }

    [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
    private void GetSellableItemPrice(HttpClient client, string currencyCode, string searchTerm, out string sellPrice, out string listPrice)
    {
      client.DefaultRequestHeaders.Remove("Currency");
      client.DefaultRequestHeaders.Add("Currency", currencyCode);
      sellPrice = "N/A";
      listPrice = "N/A";
      Uri requestUri = new Uri(this.serviceSettings["DataServiceUrl"] + "SellableItems('" + searchTerm + "')?$expand=Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))");
      HttpResponseMessage result = client.GetAsync(requestUri).Result;
      if (result.IsSuccessStatusCode)
      {
        JObject obj2 = result.Content.ReadAsAsync<JObject>().Result;
        JToken token = obj2.GetValue("Policies", StringComparison.OrdinalIgnoreCase);
        listPrice = string.Format(CultureInfo.CurrentCulture, obj2["ListPrice"]["Amount"].ToString(), new object[0]);
        foreach (JToken token2 in (IEnumerable<JToken>)token)
        {
          if (token2["@odata.type"].ToString() == "#Sitecore.Commerce.Plugin.Pricing.PurchaseOptionMoneyPolicy")
          {
            sellPrice = token2["SellPrice"].SelectToken("Amount").ToString();
          }
        }
        if (listPrice == "0")
        {
          listPrice = "N/A";
        }
      }
    }

    [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
    private void GetSellableItemVariantPrice(HttpClient client, string currencyCode, string searchTerm, string variantId, out string sellPrice, out string listPrice)
    {
      client.DefaultRequestHeaders.Remove("Currency");
      client.DefaultRequestHeaders.Add("Currency", currencyCode);
      sellPrice = "N/A";
      listPrice = "N/A";
      Uri requestUri = new Uri(this.serviceSettings["DataServiceUrl"] + "SellableItems('" + searchTerm + "')?$expand=Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))");
      HttpResponseMessage result = client.GetAsync(requestUri).Result;
      if (result.IsSuccessStatusCode)
      {
        JObject obj2 = result.Content.ReadAsAsync<JObject>().Result;
        JToken token = obj2.GetValue("Components", StringComparison.OrdinalIgnoreCase);
        listPrice = string.Format(CultureInfo.CurrentCulture, obj2["ListPrice"]["Amount"].ToString(), new object[0]);
        foreach (JToken token2 in (IEnumerable<JToken>)token)
        {
          if (token2["@odata.type"].ToString() == "#Sitecore.Commerce.Plugin.Catalog.ItemVariationsComponent")
          {
            foreach (JToken token3 in (IEnumerable<JToken>)token2["ChildComponents"])
            {
              if (token3["Id"].ToString() == variantId)
              {
                foreach (JToken token4 in (IEnumerable<JToken>)token3["Policies"])
                {
                  if (token4["@odata.type"].ToString() == "#Sitecore.Commerce.Plugin.Pricing.PurchaseOptionMoneyPolicy")
                  {
                    sellPrice = token4["SellPrice"].SelectToken("Amount").ToString();
                  }
                  if (token4["@odata.type"].ToString() == "#Sitecore.Commerce.Plugin.Pricing.ListPricingPolicy")
                  {
                    foreach (JToken token5 in (IEnumerable<JToken>)token4["Prices"])
                    {
                      if (token5.SelectToken("CurrencyCode").ToString().Equals(currencyCode))
                      {
                        listPrice = token5.SelectToken("Amount").ToString();
                      }

                    }

                  }
                }
              }
            }
          }
        }
        if (listPrice == "0")
        {
          listPrice = "N/A";
        }
      }
    }

    [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
    private JToken GetShopCurrencies(HttpClient client, string shopName)
    {
      Uri requestUri = new Uri(this.serviceSettings["DataServiceUrl"] + "Shops('" + shopName + "')?$expand=Components");
      HttpResponseMessage result = client.GetAsync(requestUri).Result;
      if (!result.IsSuccessStatusCode)
      {
        return null;
      }
      return result.Content.ReadAsAsync<JObject>().Result.GetValue("Currencies", StringComparison.OrdinalIgnoreCase);
    }

    public JsonResult UpdateListPriceForItem(string environment, string catalogName, string productId, string variantId, string currencyCode, decimal price)
    {
      Assert.IsNotNullOrEmpty(environment, "environment");
      Assert.IsNotNullOrEmpty(catalogName, "catalogName");
      Assert.IsNotNullOrEmpty(productId, "productId");
      Assert.IsNotNullOrEmpty(currencyCode, "currencyCode");
      string str = this.serviceSettings["ShopName"];
      string str2 = catalogName + "|" + productId + "|";
      if (!string.IsNullOrWhiteSpace(variantId))
      {
        str2 = str2 + variantId;
      }
      string format = "{{ \"itemId\": \"{0}\", \"prices\": [ {{ \"CurrencyCode\": \"{1}\", \"Amount\": {2} }} ] }}";
      object[] args = new object[] { str2, currencyCode, price };
      format = string.Format(CultureInfo.InvariantCulture, format, args);
      Uri requestUri = new Uri(this.serviceSettings["DataServiceUrl"] + "UpdateListPrices()");
      HttpClient client1 = new HttpClient();
      client1.DefaultRequestHeaders.Accept.Clear();
      client1.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
      client1.DefaultRequestHeaders.Add("ShopName", str);
      client1.DefaultRequestHeaders.Add("Environment", environment);
      HttpResponseMessage result = client1.PutAsync(requestUri, new StringContent(format, Encoding.UTF8, "application/json")).Result;
      Dictionary<string, object> data = new Dictionary<string, object>();
      List<string> list = new List<string>();
      if (result.IsSuccessStatusCode)
      {
        Dictionary<string, object> dictionary2 = result.Content.ReadAsAsync<Dictionary<string, object>>().Result;
        if (dictionary2.ContainsKey("ResponseCode"))
        {
          if (((string)dictionary2["ResponseCode"]) == "Ok")
          {
            data.Add("Status", "success");
          }
          else
          {
            data.Add("Status", "error");
          }
        }
        if (dictionary2.ContainsKey("Messages"))
        {
          foreach (JToken token in (JArray)dictionary2["Messages"])
          {
            string str4 = token["Code"].Value<string>();
            if (!string.IsNullOrWhiteSpace(str4) && str4.Equals("Error", StringComparison.OrdinalIgnoreCase))
            {
              string item = token["Text"].Value<string>();
              list.Add(item);
            }
          }
          data.Add("Errors", list);
        }
      }
      else
      {
        string str6 = Translate.Text("An unexpected error has occurred.");
        list.Add(str6);
        data.Add("Status", "error");
        data.Add("Errors", list);
      }
      return base.Json(data);
    }
  }
}