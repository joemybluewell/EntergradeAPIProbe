using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace EntergradeAPIProbe.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CityInfoController(ILogger<CityInfoController> logger, HttpClient httpClient, IConfiguration config) : ControllerBase
    {
        const string serverErrorMsg = "Server Error: We were unable to process your request. Please try again later.";

        /// <summary>
        /// Look up in the first API, providing the city's zipcode to obtain the city name.
        /// Then provide the city name to the second API to get the city's weather and return it all to the caller.
        /// Test cases: 1010, 33823, 33930, 34242
        /// </summary>
        /// <param name="zipCode">The zip code to look up.</param>
        /// <returns>The weather information for the given zip code.</returns>
        [HttpGet(Name = "GetCityWeather")]
        public async Task<IActionResult> Get(string zipCode)
        {
            try
            {
                //Ensure the initial params are well set
                var stringLengths = config.GetSection("StringParamConfig");
                var min = stringLengths.GetValue<int>("MinLength");
                var max = stringLengths.GetValue<int>("MaxLength");

                if (min > 0 && max > 0)
                {
                    //Ensure the provided zipcode matches the standards
                    if (zipCode.Length < min || zipCode.Length > 10)
                    {
                        return BadRequest($"Invalid zip code format. Zip code should be between {min} and {max} characters.");
                    }

                    var endPoints = config.GetSection("EndPoints");
                    var zipEndpoint = endPoints.GetValue<string>("Zipcode");
                    var weatherEndpoint = endPoints.GetValue<string>("Weather");

                    // Make sure the endpoints are populated!
                    if (string.IsNullOrEmpty(zipEndpoint) || string.IsNullOrEmpty(weatherEndpoint))
                    {
                        logger.LogError("API endpoints are not configured properly.");
                        return StatusCode(500, "API endpoints are missing in the configuration.");
                    }

                    var cityInfo = await httpClient.GetFromJsonAsync<CityData>($"{zipEndpoint}{zipCode}");

                    if (cityInfo is not null)
                    {
                        var weatherData = await httpClient.GetFromJsonAsync<CityData>($"{weatherEndpoint}{cityInfo.CityName}");

                        if (weatherData is not null)
                        {
                            cityInfo.CurrentWeather = weatherData.CurrentWeather;
                            return Ok(cityInfo);
                        }
                    }

                    return Problem("The remote service is currently unavailable. Please try again later.");
                }
                else
                {
                    logger.LogError("Configuration file has issues properly.");
                    return StatusCode(500, serverErrorMsg);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e.Message + e.StackTrace);
                return StatusCode(500, serverErrorMsg);
            }
        }
    }
}
