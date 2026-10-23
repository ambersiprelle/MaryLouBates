module ChiAha.ProductSite.Program

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Falco
open Falco.Routing
open ChiAha.ProductSite.Configuration
open ChiAha.ProductSite.Handlers

[<EntryPoint>]
let main args =
    let config = loadConfig ()
    printConfigStatus config
    Db.init config.DbPath

    let builder = WebApplication.CreateBuilder(args)
    let app = builder.Build()

    // One address for search engines: www.maryloubates.com -> maryloubates.com
    app.Use(fun (ctx: HttpContext) (next: RequestDelegate) ->
        if ctx.Request.Host.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) then
            let target = sprintf "https://%s%s%s" (ctx.Request.Host.Host.Substring 4) (string ctx.Request.Path) (string ctx.Request.QueryString)
            ctx.Response.StatusCode <- 301
            ctx.Response.Headers.Location <- target
            Task.CompletedTask
        else next.Invoke ctx) |> ignore

    app.UseDefaultFiles() |> ignore
    app.UseStaticFiles() |> ignore
    app.UseRouting() |> ignore

    let endpoints =
        [
            get "/health" healthCheck
            post "/contact" (contactHandler config)
            post "/subscribe" (subscribeHandler config)
            get "/subscribe/confirm" (confirmHandler config)
            get "/unsubscribe" unsubscribeHandler
            get "/admin/subscribers.csv" (subscribersCsvHandler config)
        ]

    app.UseFalco(endpoints) |> ignore

    app.Run()
    0
