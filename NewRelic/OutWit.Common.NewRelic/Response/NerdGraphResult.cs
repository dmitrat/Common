using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace OutWit.Common.NewRelic.Response
{
    internal sealed class NerdGraphResult
    {
        public List<Dictionary<string, JsonElement>> Results { get; set; } = new();
    }
}
