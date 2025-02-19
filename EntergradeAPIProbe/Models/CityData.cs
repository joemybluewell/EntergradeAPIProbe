using System.Text.Json.Serialization;

namespace EntergradeAPIProbe
{
    public class CityData
    { 
        public string CityName { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;

        [JsonPropertyName("Weather")]
        public string CurrentWeather { get; set; } = string.Empty;
    }
}
