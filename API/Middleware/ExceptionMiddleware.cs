using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using API.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _Ilogger;
        private readonly IHostEnvironment _env;
        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> Ilogger, IHostEnvironment env)
        {
            this._env = env;
            this._Ilogger = Ilogger;
            this._next = next;
        }
        public async Task InvokeAsync(HttpContext http)
        {
            try
            {
               await _next(http);
            }
            catch(Exception ex)
            {
                _Ilogger.LogError(ex,ex.Message);
               http.Response.ContentType="application/json";
               http.Response.StatusCode=(int)HttpStatusCode.InternalServerError;

               var response=_env.IsDevelopment()?new ApiException(http.Response.StatusCode,ex.Message,ex.StackTrace?.ToString()):
               new ApiException(http.Response.StatusCode,"Internal Server Error");
               var option= new JsonSerializerOptions{PropertyNamingPolicy = JsonNamingPolicy.CamelCase};

               var json=JsonSerializer.Serialize(response,option);
               await http.Response.WriteAsync(json);

            }
        }
    }
}