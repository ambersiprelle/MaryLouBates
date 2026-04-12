module ChiAha.ProductSite.Handlers

open Falco

let healthCheck : HttpHandler =
    fun ctx -> Response.ofPlainText "OK" ctx
