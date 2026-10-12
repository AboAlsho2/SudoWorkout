using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TrainingService.Tests.IntegrationTests.Infrastructure
{


        public sealed class FakeWorkoutServiceHandler(
            FakeWorkoutServiceResponses responses) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                var path = request.RequestUri!.AbsolutePath;

                if (path.Contains("/api/members/"))
                {
                    return Task.FromResult(CreateResponse(
                        responses.MemberPlanStatus,
                        responses.MemberPlan));
                }

                if (path.Contains("/api/workout-plans/"))
                {
                    return Task.FromResult(CreateResponse(
                        responses.ExercisesStatus,
                        responses.Exercises));
                }

                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            private static HttpResponseMessage CreateResponse(
                HttpStatusCode status,
                object? body)
            {
                var response = new HttpResponseMessage(status);

                if (body is not null)
                {
                    response.Content = System.Net.Http.Json.JsonContent.Create(body);
                }

                return response;
            }
        }


    public sealed class FakeWorkoutServiceResponses
    {
        public HttpStatusCode MemberPlanStatus { get; set; } =
            HttpStatusCode.OK;

        public object? MemberPlan { get; set; }

        public HttpStatusCode ExercisesStatus { get; set; } =
            HttpStatusCode.OK;

        public object? Exercises { get; set; }
    }


}

