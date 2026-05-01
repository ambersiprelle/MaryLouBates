module ChiAha.ProductSite.Handlers

open System
open System.Net.Http
open System.Net.Http.Headers
open System.Text
open System.Text.Json
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Falco
open ChiAha.ProductSite.Configuration

let healthCheck : HttpHandler =
    fun ctx -> Response.ofPlainText "OK" ctx

let private httpClient = new HttpClient()

let private htmlEncode (s: string) =
    System.Net.WebUtility.HtmlEncode(s)

let private sendViaResend (config: ProductConfig) (name: string) (fromEmail: string) (message: string) =
    task {
        if String.IsNullOrEmpty config.ResendApiKey then
            return Error "RESEND_API_KEY not configured"
        else
            let safeName = if String.IsNullOrWhiteSpace name then "(no name)" else name.Trim()
            let safeMsg = if String.IsNullOrWhiteSpace message then "(empty message)" else message.Trim()
            let subject = sprintf "maryloubates.com contact form — %s" safeName
            let body =
                sprintf "<p><strong>From:</strong> %s &lt;%s&gt;</p><p><strong>Message:</strong></p><p style=\"white-space:pre-wrap;\">%s</p>"
                    (htmlEncode safeName)
                    (htmlEncode fromEmail)
                    (htmlEncode safeMsg)
            let payload =
                {| ``from`` = config.ContactFromEmail
                   ``to`` = [| config.ContactToEmail |]
                   reply_to = fromEmail
                   subject = subject
                   html = body |}
            let json = JsonSerializer.Serialize payload
            use req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
            req.Headers.Authorization <- AuthenticationHeaderValue("Bearer", config.ResendApiKey)
            req.Content <- new StringContent(json, Encoding.UTF8, "application/json")
            let! resp = httpClient.SendAsync(req)
            let! responseBody = resp.Content.ReadAsStringAsync()
            if resp.IsSuccessStatusCode then
                return Ok ()
            else
                return Error (sprintf "Resend %d: %s" (int resp.StatusCode) responseBody)
    }

let private redirectTo (location: string) (ctx: HttpContext) : Task =
    ctx.Response.StatusCode <- 303
    ctx.Response.Headers.Append("Location", location)
    Task.CompletedTask

let contactHandler (config: ProductConfig) : HttpHandler =
    fun ctx ->
        task {
            let! form = Request.getForm ctx
            let name = form.GetString("name", "")
            let email = form.GetString("email", "")
            let message = form.GetString("message", "")
            let honeypot = form.GetString("website", "")

            if not (String.IsNullOrEmpty honeypot) then
                return! redirectTo "/?contact=ok#contact" ctx
            elif String.IsNullOrWhiteSpace email then
                return! redirectTo "/?contact=err#contact" ctx
            else
                let! result = sendViaResend config name email message
                match result with
                | Ok () ->
                    return! redirectTo "/?contact=ok#contact" ctx
                | Error err ->
                    eprintfn "[contact] send failed: %s" err
                    return! redirectTo "/?contact=err#contact" ctx
        }
