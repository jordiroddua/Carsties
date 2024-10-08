using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);
var  CarsitesAllowSpecificOrigins = "_carsitesAllowSpecificOrigins";

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// https://microsoft.github.io/reverse-proxy/articles/authn-authz.html
// OAuth2, OpenIdConnect.
// The result of the authentication will be an authentication cookie. That cookie will flow to the destination server as a normal request header.

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.Authority = builder.Configuration["IdentityServiceUrl"];
        options.RequireHttpsMetadata = false; // identity service runs on http
        options.TokenValidationParameters.ValidateAudience = false;
        options.TokenValidationParameters.NameClaimType = "username";
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: CarsitesAllowSpecificOrigins,
                      policy  =>
                      {
                          policy.WithOrigins(builder.Configuration["ClientApp"]);
                          policy.AllowAnyMethod();
                          policy.AllowAnyHeader();
                          policy.AllowCredentials();
                        //   policy.WithHeaders("authorization", "content-type");
                      });
});

var app = builder.Build();

app.UseCors(CarsitesAllowSpecificOrigins);

app.MapReverseProxy();

app.UseAuthentication();

app.UseAuthorization();

app.Run();
