using KursovWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();


builder.Services.AddSingleton<VoiceAssistant>(sp =>
{
    var assistant = new VoiceAssistant();
    //assistant.Start();
    
    return assistant;
});

var app = builder.Build();

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStopping.Register(() =>
{

});

if (!app.Environment.IsDevelopment())
{
    // Middleware для обработки исключений
    app.UseExceptionHandler("/Home/Error");

    // Middleware для перенаправления HTTP-запросов на HTTPS
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Добавление middleware для работы с cookies
app.UseCookiePolicy(new CookiePolicyOptions
{
    HttpOnly = HttpOnlyPolicy.Always,
    Secure = CookieSecurePolicy.Always,
    MinimumSameSitePolicy = SameSiteMode.Strict
});

// Middleware для обслуживания статических файлов
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

