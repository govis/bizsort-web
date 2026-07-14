using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class Geolocation
{
    [JsonPropertyName(""lat"")]
    public double Lat { get; set; }
    [JsonPropertyName(""lng"")]
    public double Lng { get; set; }
}

class Program
{
    static void Main()
    {
        string json = ""{\""searchNear\"":{\""text\"":\""Toronto, ON, Canada\"",\""lat\"":43.6548253,\""lng\"":-79.388447}}"";
        try {
            var doc = JsonDocument.Parse(json);
            var geo = JsonSerializer.Deserialize<Geolocation>(doc.RootElement.GetProperty(""searchNear""));
            Console.WriteLine($""Success: Lat={geo.Lat}, Lng={geo.Lng}"");
        } catch (Exception ex) {
            Console.WriteLine(""Error: "" + ex.Message);
        }
    }
}
