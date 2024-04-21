namespace TypeOneFoodJournal.Services.External
{
    using System;
    using System.Net.Http.Headers;
    using System.Net.Http;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;
    using TypeOneFoodJournal.Data;

    public class TandemService : IInsulinPumpService
    {
        private readonly HttpClient httpClient;

        public TandemApiOptions Options { get; }

        private const string baseUrl = "https://tdcservices.tandemdiabetes.com/";

        public string Token { get; private set; }
        public string UserId { get; private set; }
        public string Scope { get; set; }

        public TandemService(HttpClient httpClient, IOptions<TandemApiOptions> options)
        {
            this.httpClient = httpClient;
            this.Options = options.Value;
        }

        public async Task<ReadingList> GetInsulinPumpDataAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                IDictionary<string, string> keyValuePairs = BuildPumpEventParameters();

                // I'm unsure what false does here, but it is needed.
                string tandemUrl = baseUrl.BuildUrlWithDates(TandemEndpoints.PumpEvents.Path, startDate, endDate, keyValuePairs, "false");

                string content = await GetInsulinPumpDataAsync(tandemUrl);

                // Return the parsed data
                return ExtractReadingList(content);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task Login(string username, string password)
        {
            try
            {
                Dictionary<string, string> credentials = BuildCredentials(username, password);

                string content = await PostLogin(credentials);

                TandemTokenResponse? tokenResponse = ExtractTokenResponse(content);

                // Store the token for future requests
                this.UpdateToken(tokenResponse.AccessToken, tokenResponse.User!.Id);
            }
            catch (HttpRequestException ex)
            {
                // Handle any exceptions thrown during the API call
                throw new Exception("Error logging in to Tandem Diabetes API", ex);
            }
            catch (JsonException je)
            {
                // Handle any exceptions thrown during the API call
                throw new Exception("Error parsing Tandem Diabetes API response", je);
            }
        }

        public void UpdateToken(string token, string userId)
        {
            this.Token = token;
            this.UserId = userId;

            // Add the token to the HTTP client.
            this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.Token);
        }

        private static ReadingList ExtractReadingList(string content)
        {

            // Deserialize the Json response to obtain the token
            ReadingList? readingList = JsonConvert.DeserializeObject<ReadingList>(content);
            if (readingList == null)
            {
                // Invalid response
                throw new Exception("Tandem API returned an unexpected token.");
            }

            return readingList;
        }

        private async Task<string> GetInsulinPumpDataAsync(string tandemUrl)
        {
            // Make the external API call
            var response = await httpClient.GetAsync(tandemUrl);

            if (response.IsSuccessStatusCode == false)
            {
                // Handle the error response
                throw new Exception($"Tandem Diabetes API returned error status code: {response.StatusCode}");
            }

            // Parse the response content
            var content = await response.Content.ReadAsStringAsync();
            return content;
        }

        private IDictionary<string, string> BuildPumpEventParameters()
        {
            return new Dictionary<string, string>()
                {
                    { TandemEndpoints.PumpEvents.UserId, this.UserId },
                };
        }

        private Dictionary<string, string> BuildCredentials(string username, string password)
        {
            // Create a dictionary with the credentials
            var credentials = new Dictionary<string, string>
            {
                    { TandemEndpoints.Login.Username, username },
                    { TandemEndpoints.Login.Password, password },
                    { TandemEndpoints.Login.GrantType, "password" },
            };

            if (string.IsNullOrEmpty(this.Scope) == false)
            {
                credentials.Add(TandemEndpoints.Login.Scope, this.Scope);
            }

            return credentials;
        }

        private static TandemTokenResponse ExtractTokenResponse(string content)
        {

            // Deserialize the JSON response to obtain the token
            TandemTokenResponse? tokenResponse = JsonConvert.DeserializeObject<TandemTokenResponse>(content);

            if (tokenResponse == null || tokenResponse.User == null)
            {
                // Invalid response
                throw new HttpRequestException("Tandem API returned an unexpected token.");
            }

            return tokenResponse;
        }

        private async Task<string> PostLogin(Dictionary<string, string> credentials)
        {
            // Add basic authentication header to the request
            var authHeader = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{this.Options.ApplicationUsername}:{this.Options.ApplicationPassword}")));
            httpClient.DefaultRequestHeaders.Authorization = authHeader;

            // Send a POST request to obtain a bearer token
            var response = await httpClient.PostAsync(TandemEndpoints.Login.Path, new FormUrlEncodedContent(credentials));

            // Check the response status code
            if (response.IsSuccessStatusCode == false)
            {
                // Handle the error response
                throw new Exception($"Tandem Diabetes API returned error status code: {response.StatusCode}");
            }

            return await response.Content.ReadAsStringAsync();
        }
    }
}
