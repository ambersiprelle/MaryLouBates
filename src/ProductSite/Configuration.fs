module ChiAha.ProductSite.Configuration

open System

type ProductConfig = {
    ProductName: string
}

let private getEnv key fallback =
    match Environment.GetEnvironmentVariable(key) with
    | null | "" -> fallback
    | v -> v

let loadConfig () =
    {
        ProductName = getEnv "PRODUCT_NAME" "MaryLouBates"
    }

let printConfigStatus (config: ProductConfig) =
    printfn "[%s] Configuration loaded" config.ProductName
