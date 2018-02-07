using AmsMigrator.DTO.AMS1;
using AmsMigrator.DTO.Okapi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

using AmsMigrator.Models;

namespace AmsMigrator
{
    internal static class Ex
    {
        public static Element GetElementByType(this MaterialStub stub, MaterialElementType elementType)
        {
            return stub
                .Elements
                .FirstOrDefault(el => el.Type?.Equals(elementType.ToString(), StringComparison.OrdinalIgnoreCase) ?? false);
        }

        public static Datum GetFirstDataItem(this LogoInfo info, string name) =>
            info?.Data?.FirstOrDefault(d => d.Name == name);

        public static IEnumerable<Datum> GetDataItems(this LogoInfo info, Func<Datum, bool> filter) =>
            info?.Data?.Where(filter).ToList();

        public static HttpClient Configure(this HttpClient client, string token, int timeout = 60)
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.Timeout = TimeSpan.FromSeconds(timeout);

            return client;
        }
    }
}
