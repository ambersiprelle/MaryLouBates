module ChiAha.ProductSite.Configuration

open System

type ProductConfig = {
    ProductName: string
    ResendApiKey: string
    ContactToEmail: string
    ContactFromEmail: string
}

let private getEnv key fallback =
    match Environment.GetEnvironmentVariable(key) with
    | null | "" -> fallback
    | v -> v

let loadConfig () =
    {
        ProductName = getEnv "PRODUCT_NAME" "MaryLouBates"
        ResendApiKey = getEnv "RESEND_API_KEY" ""
        ContactToEmail = getEnv "CONTACT_TO_EMAIL" "marybatesroses@gmail.com"
        ContactFromEmail = getEnv "CONTACT_FROM_EMAIL" "onboarding@resend.dev"
    }

let printConfigStatus (config: ProductConfig) =
    let resendStatus = if String.IsNullOrEmpty config.ResendApiKey then "MISSING" else "set"
    printfn "[%s] Configuration loaded (RESEND_API_KEY: %s)" config.ProductName resendStatus
