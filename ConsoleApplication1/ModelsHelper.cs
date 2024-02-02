using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

public static class ModelsHelper
{
    static readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.Indented,
    };
    public static string ToJson(this Object obj)
    {
        return JsonConvert.SerializeObject(obj, jsonSerializerSettings);
    }
    public static T FromJson<T>(this string json)
    {
        return JsonConvert.DeserializeObject<T>(json, jsonSerializerSettings);
    }
}