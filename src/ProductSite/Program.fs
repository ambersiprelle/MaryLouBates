module ChiAha.ProductSite.Program

open Microsoft.AspNetCore.Builder
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
