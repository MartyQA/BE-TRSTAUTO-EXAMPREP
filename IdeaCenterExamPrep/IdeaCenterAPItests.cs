using IdeaCenterExamPrep.Models;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using static System.Net.WebRequestMethods;



namespace IdeaCenterExamPrep
{
    [TestFixture]
    public class IdeaCenterAPITests
    {
        private RestClient client;
        private const string BaseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:84";
        private static string? lastCreatedIdeaId;

        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiI5OTdlYzUzMC01MGVhLTQzYjYtYmFiMS1iNTkwNjY1OWEzZDciLCJpYXQiOiIwOC8xNC8yMDI1IDE3OjM4OjI1IiwiVXNlcklkIjoiNDg0ZDZhZjctYzllOS00N2MzLWQyY2QtMDhkZGQ0ZTA4YmQ4IiwiRW1haWwiOiJtdnZAZ21haWwuY29tIiwiVXNlck5hbWUiOiJtdnYiLCJleHAiOjE3NTUyMTQ3MDUsImlzcyI6IklkZWFDZW50ZXJfQXBwX1NvZnRVbmkiLCJhdWQiOiJJZGVhQ2VudGVyX1dlYkFQSV9Tb2Z0VW5pIn0.sXeVwFTGGYNa7rzSjttQBkOHCd43HLDQm4uHDvlwoYs";

        private const string LoginEmail = "mvv@gmail.com";
        private const string LoginPass = "123456";


        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else 
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPass);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);

           
        }

        private string GetJwtToken(string loginEmail, string loginPass)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { loginEmail, loginPass });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var cotent = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = cotent.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token)) 
                {
                    throw new InvalidOperationException("Faild to retrive JWT token from the response");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Content: {response.Content}");
            }

        }
        //All test here

        [Order(1)]
        [Test]

        public void CreateIdea_WithRequiredFields_ShouldReturnSuccess()
        {
            var ideaReqest = new IdeaDTO
            {
                Title = "Test Idea",
                Description = "Description",
                Url = ""
            };

            var reqest = new RestRequest("/api/Idea/Create", Method.Post);
            reqest.AddJsonBody(ideaReqest);
            var response = this.client.Execute(reqest);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK");
            Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));
        }

        [Order(2)]
        [Test]
        public void GetAllIdeas_ShouldReturnListOfIdeas()
        {
            var request = new RestRequest("/api/Idea/All", Method.Get);
            var response = this.client.Execute(request);

            var responsItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responsItems, Is.Not.Null);
            Assert.That(responsItems, Is.Not.Empty);

            lastCreatedIdeaId = responsItems.LastOrDefault()?.Id;
        }

        [Order(3)]
        [Test]

        public void EditExistingIdea_ShouldReturnSuccess()
        {
            var editRequest = new IdeaDTO
            {
                Title = "Edited Idea",
                Description = "This is an updated test idea description.",
                Url = ""
            };

            var request = new RestRequest($"/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponse.Msg, Is.EqualTo("Edited successfully"));

        }

        [Order(4)]
        [Test]

        public void DeleteIdea_ShouldReturnSuccess()
        {
            var request = new RestRequest($"/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("The idea is deleted!"));
        }

        [Order(5)]
        [Test]

        public void CreateIdea_WithMissingFields_ShouldReturnBadRequest()
        {
            var ideaRequest = new IdeaDTO
            {
                Title = "Test Idea with Missing Fields",
                Description = null,
                Url = null 
            };
            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            
        }

        [Order(6)]
        [Test]

        public void EditNonExistingIdea_ShouldReturnNotFound()
        {
            string nonExistingIdeaId = "non-existing-id";
            var editRequest = new IdeaDTO
            {
                Title = "Edited Non-Existing Idea",
                Description = "This idea does not exist.",
                Url = ""
            };
            var request = new RestRequest($"/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", nonExistingIdeaId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }




























        [OneTimeTearDown]
        public void Teardown()
        {
            this.client?.Dispose();
        }

        
    }
}