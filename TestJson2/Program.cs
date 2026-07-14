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
        string json = ""{\""text\"":\""Toronto, ON, Canada\"",\""lat\"":43.6548253,\""lng\"":-79.388447}"";
        try {
            var obj = JsonSerializer.Deserialize<Geolocation>(json);
            Console.WriteLine($""Success: Lat={obj.Lat}, Lng={obj.Lng}"");
        } catch (Exception ex) {
            Console.WriteLine(ex.ToString());
        }
    }
}
