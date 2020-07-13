using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json.Linq;
using Formatting = Newtonsoft.Json.Formatting;

namespace App.BitwardenConvert
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            ConvertFromMsdn(args);
        }

        private static void ConvertFromMsdn(string[] args)
        {
            foreach (var input in args)
            {
                var inputContent = File.ReadAllText(input);
                inputContent = Regex.Replace(inputContent, @"(<a[^>]+>|</a>)", string.Empty);

                var xml = new XmlDocument();
                xml.LoadXml(inputContent);

                var names = new List<string>();
                var serials = new List<string>();

                var keys = new List<JObject>();
                foreach (XmlNode xmlProduct in xml["root"]["YourKey"])
                {
                    var valueName = xmlProduct.Attributes["Name"].Value;
                    foreach (XmlNode xmlKey in xmlProduct.ChildNodes)
                    {
                        var valueKey = $"{xmlKey.InnerText}".Trim();
                        var valueType = $"{xmlKey.Attributes["Type"]?.Value}".Trim();
                        var valueClaimedDate = $"{xmlKey.Attributes["ClaimedDate"]?.Value}".Trim();
                        if (Regex.IsMatch(valueKey, @"^[\d\w-]+$"))
                        {
                            var valueNameForKey = $"{valueName} {valueType} {valueClaimedDate}".Trim();
                            names.Add(valueNameForKey);

                            var name = $"Serial: {valueNameForKey}".Trim();
                            var value = $"{valueKey}".Trim();

                            var serial = $"{name}{value}";
                            if (serials.All(a => a != serial))
                            {
                                serials.Add(serial);

                                keys.Add(new JObject
                                {
                                    ["name"] = $"{name} #{names.Count(a => a == valueNameForKey).ToString("000")}",
                                    ["value"] = value
                                });
                            }
                        }
                    }
                }

                var item = new JObject();
                item["type"] = 2;
                item["name"] = "Microsoft MSDN Product Keys";
                item["notes"] =
                    $"Subscription: {xml["root"]["YourSubscription"]["Subscription"].Attributes["Name"].Value}, {xml["root"]["YourSubscription"]["Subscription"]["SubscriptionGuid"].InnerText}";
                item["fields"] = new JArray(keys.OrderBy(a => a["name"].ToString()).ToArray());
                item["secureNote"] = new JObject
                {
                    ["type"] = 0
                };
                item["collectionIds"] = null;

                var items = new JObject();
                items["items"] = new JArray(new List<JObject> {item}.ToArray());

                var output = $"{input}.json";
                File.WriteAllText(output, items.ToString(Formatting.Indented));
            }
        }
    }
}