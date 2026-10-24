module ChiAha.ProductSite.Spam

open System
open System.Collections.Concurrent
open System.Security.Cryptography
open System.Text
open Microsoft.AspNetCore.Http
open Falco
open ChiAha.ProductSite.Configuration

// Bot defence for the contact and book news forms. A form only counts when:
//   - the honeypot field is empty,
//   - it carries a signed token the page fetched with JavaScript from /form-token
//     (bots that post straight to the endpoint never have one),
//   - at least MinSeconds passed between loading the page and submitting,
//   - the IP has not sent more than MaxPerHour forms in the last hour,
//   - (contact only) the message has no links.
// Blocked posts get the normal "thank you" redirect so bots learn nothing, and no email is sent.

let private MinSeconds = 3.0
let private MaxAgeHours = 24.0
let private MaxPerHour = 3

let private signingKey (config: ProductConfig) =
    SHA256.HashData(Encoding.UTF8.GetBytes("mlb-form-token:" + config.ResendApiKey))

let private sign (config: ProductConfig) (issued: string) =
    use h = new HMACSHA256(signingKey config)
    Convert.ToHexString(h.ComputeHash(Encoding.UTF8.GetBytes issued)).ToLowerInvariant()

/// A token stamped with the time the page asked for it.
let issueToken (config: ProductConfig) =
    let issued = string (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
    issued + "." + sign config issued

let private checkToken (config: ProductConfig) (token: string) : string option =
    match token.Split('.') with
    | [| issued; signature |] ->
        let expected = sign config issued
        if not (CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(signature), Encoding.UTF8.GetBytes(expected))) then
            Some "bad token"
        else
            match Int64.TryParse issued with
            | true, ms ->
                let age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds ms
                if age.TotalSeconds < MinSeconds then Some (sprintf "too fast (%.1fs)" age.TotalSeconds)
                elif age.TotalHours > MaxAgeHours then Some "token expired"
                else None
            | _ -> Some "bad token"
    | _ -> Some "no token"

let private recent = ConcurrentDictionary<string, DateTime list>()

let private overLimit (ip: string) =
    let cutoff = DateTime.UtcNow.AddHours(-1.0)
    let hits =
        recent.AddOrUpdate(ip, [ DateTime.UtcNow ], fun _ old -> DateTime.UtcNow :: (old |> List.filter (fun t -> t > cutoff)))
    List.length hits > MaxPerHour

let clientIp (ctx: HttpContext) =
    match string ctx.Request.Headers["Fly-Client-IP"] with
    | "" -> (match ctx.Connection.RemoteIpAddress with null -> "unknown" | a -> string a)
    | ip -> ip

let private hasLink (text: string) =
    let t = text.ToLowerInvariant()
    t.Contains("http://") || t.Contains("https://") || t.Contains("www.") || t.Contains("[url")

/// Why this form should be dropped, or None when it looks like a real person.
let blockReason (config: ProductConfig) (ctx: HttpContext) (form: FormData) (message: string) : string option =
    if form.GetString("website", "") <> "" then Some "honeypot"
    else
        match checkToken config (form.GetString("ft", "")) with
        | Some reason -> Some reason
        | None ->
            if hasLink message then Some "link in message"
            elif overLimit (clientIp ctx) then Some "rate limit"
            else None
