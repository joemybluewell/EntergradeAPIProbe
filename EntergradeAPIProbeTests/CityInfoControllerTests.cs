using EntergradeAPIProbe.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using Xunit;


namespace EntergradeAPIProbe.Test
{
    public class CityInfoControllerTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly CityInfoController _controller;
        private readonly Mock<ILogger<CityInfoController>> _mockLogger;

        /// <summary>
        /// Initializes the test class by setting up mocked dependencies, including an HTTP client and logger.
        /// </summary>
        public CityInfoControllerTests()
        {
            _mockLogger = new Mock<ILogger<CityInfoController>>(); // Mock logger
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://test.com/")
            };
            _controller = new CityInfoController(_mockLogger.Object, _httpClient, null); // Use mocked logger
        }


        /// <summary>
        /// Configures a mock HTTP response for a given request URI, status code, and optional response data.
        /// </summary>
        /// <param name="requestUri">The API endpoint to be mocked.</param>
        /// <param name="response">The response object to be returned.</param>
        /// <param name="statusCode">The HTTP status code for the response (default: 200 OK).</param>
        private void SetupHttpResponse(string requestUri, object response, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains(requestUri)),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = response != null
                        ? new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
                        : null
                });
        }

        /// <summary>
        /// Tests the Get method when an invalid zip code is provided. 
        /// Ensures that a BadRequest response is returned with an appropriate error message.
        /// </summary>
        [Fact]
        public async Task Get_InvalidZipCode_ReturnsBadRequest()
        {
            var result = await _controller.Get("1");
            var badRequestResult = Assert.IsType<ObjectResult>(result);
            Assert.Contains("We were unable to process your request.", badRequestResult.Value.ToString());
        }

        /// <summary>
        /// Tests the Get method with a valid zip code and ensures that valid city data is returned.
        /// </summary>
        [Fact]
        public async Task Get_ValidZipCode_ReturnsCityData()
        {
            var zipCode = "12345";
            var cityData = new CityData { CityName = "TestCity", CurrentWeather = "Sunny" };

            SetupHttpResponse($"zipcode/{zipCode}", cityData);
            SetupHttpResponse($"weather/TestCity", cityData);

            var result = await _controller.Get(zipCode);
            var okResult = Assert.IsType<ObjectResult>(result);
            var returnedCityData = Assert.IsType<string>(okResult.Value);
            Assert.NotNull(returnedCityData);
        }

        /// <summary>
        /// Tests the Get method when the API request for zip code lookup fails. 
        /// Ensures that a 500 Internal Server Error is returned.
        /// </summary>
        [Fact]
        public async Task Get_ApiFailure_ReturnsServerError()
        {
            var zipCode = "12345";

            SetupHttpResponse($"zipcode/{zipCode}", null, HttpStatusCode.InternalServerError);

            var result = await _controller.Get(zipCode);
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
        }

        /// <summary>
        /// Tests the Get method when the API request for weather data fails. 
        /// Ensures that a 500 Internal Server Error is returned.
        /// </summary>
        [Fact]
        public async Task Get_WeatherApiFailure_ReturnsServerError()
        {
            var zipCode = "12345";
            var cityData = new CityData { CityName = "TestCity" };

            SetupHttpResponse($"zipcode/{zipCode}", cityData);
            SetupHttpResponse($"weather/TestCity", null, HttpStatusCode.InternalServerError);

            var result = await _controller.Get(zipCode);
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
        }
    }
}