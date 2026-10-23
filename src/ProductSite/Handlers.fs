module ChiAha.ProductSite.Handlers

open System
open System.Net.Http
open System.Net.Http.Headers
open System.Text
open System.Text.Json
open System.Threading.Tasks
open System.Security.Cryptography
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

/// Send one email through Resend.
let private sendEmail (config: ProductConfig) (toEmail: string) (replyTo: string) (subject: string) (html: string) =
    task {
        if String.IsNullOrEmpty config.ResendApiKey then
            return Error "RESEND_API_KEY not configured"
        else
            let payload =
                {| ``from`` = (if config.ContactFromEmail.Contains("<") then config.ContactFromEmail else sprintf "Mary Lou Bates <%s>" config.ContactFromEmail)
                   ``to`` = [| toEmail |]
                   reply_to = replyTo
                   subject = subject
                   html = html |}
            use req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
            req.Headers.Authorization <- AuthenticationHeaderValue("Bearer", config.ResendApiKey)
            req.Content <- new StringContent(JsonSerializer.Serialize payload, Encoding.UTF8, "application/json")
            let! resp = httpClient.SendAsync(req)
            let! responseBody = resp.Content.ReadAsStringAsync()
            if resp.IsSuccessStatusCode then return Ok ()
            else return Error (sprintf "Resend %d: %s" (int resp.StatusCode) responseBody)
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

let private isPlausibleEmail (email: string) =
    let e = email.Trim()
    e.Length <= 254 && e.Contains("@") && e.LastIndexOf('.') > e.IndexOf('@') && not (e.Contains(" "))

let private emailShell (inner: string) =
    sprintf "<div style=\"font-family:Georgia,serif;font-size:16px;line-height:1.6;color:#333;max-width:560px;\">%s</div>" inner

let private notifyNewReader (config: ProductConfig) (email: string) (name: string) =
    task {
        let html =
            emailShell (
                sprintf "<p><strong>%s</strong> &lt;%s&gt; signed up for news about your book on maryloubates.com.</p>"
                    (htmlEncode (if String.IsNullOrWhiteSpace name then "(no name)" else name.Trim())) (htmlEncode email))
        let! result = sendEmail config config.ContactToEmail email "New reader signed up for book news" html
        match result with
        | Error err -> eprintfn "[subscribe] owner notice failed: %s" err
        | Ok () -> ()
    }

/// POST /subscribe — add the reader straight to the list and tell Mary Lou.
let subscribeHandler (config: ProductConfig) : HttpHandler =
    fun ctx ->
        task {
            let! form = Request.getForm ctx
            let name = form.GetString("name", "")
            let email = form.GetString("email", "").Trim()
            let source = form.GetString("source", "site")
            let honeypot = form.GetString("website", "")

            if not (String.IsNullOrEmpty honeypot) then
                return! redirectTo "/?news=confirmed#news" ctx
            elif not (isPlausibleEmail email) then
                return! redirectTo "/?news=err#news" ctx
            else
                if Db.subscribe email name source then
                    printfn "[subscribe] added %s (source: %s)" email source
                    do! notifyNewReader config email name
                    return! redirectTo "/?news=confirmed#news" ctx
                else
                    return! redirectTo "/?news=already#news" ctx
        }

/// GET /subscribe/confirm?t=... — left over from the double opt-in version; still honours
/// any confirm links already sent.
let confirmHandler (config: ProductConfig) : HttpHandler =
    fun ctx ->
        task {
            let token = (Request.getQuery ctx).GetString("t", "")
            match (if token = "" then None else Db.confirm token) with
            | Some (email, name) ->
                printfn "[subscribe] confirmed %s" email
                do! notifyNewReader config email name
                return! redirectTo "/?news=confirmed#news" ctx
            | None -> return! redirectTo "/?news=err#news" ctx
        }

/// GET /unsubscribe?t=... — one-click unsubscribe.
let unsubscribeHandler : HttpHandler =
    fun ctx ->
        let token = (Request.getQuery ctx).GetString("t", "")
        match (if token = "" then None else Db.unsubscribe token) with
        | Some (email, _) ->
            printfn "[subscribe] unsubscribed %s" email
            redirectTo "/?news=unsubscribed#news" ctx
        | None -> redirectTo "/?news=err#news" ctx

/// GET /admin/subscribers.csv — basic auth with ADMIN_PASSWORD; 404 when unset.
let subscribersCsvHandler (config: ProductConfig) : HttpHandler =
    fun ctx ->
        let expected = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("admin:" + config.AdminPassword))
        let given = string ctx.Request.Headers.Authorization
        if String.IsNullOrEmpty config.AdminPassword then
            ctx.Response.StatusCode <- 404
            Task.CompletedTask
        elif not (CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(given), Encoding.UTF8.GetBytes(expected))) then
            ctx.Response.StatusCode <- 401
            ctx.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"subscribers\"")
            Task.CompletedTask
        else
            let q (v: string) = "\"" + v.Replace("\"", "\"\"") + "\""
            let lines =
                Db.listSubscribers ()
                |> List.map (fun s -> String.Join(",", [ q s.Email; q s.Name; q s.Status; q s.Source; q s.CreatedAt; q s.ConfirmedAt ]))
            let csv = String.Join("\n", "email,name,status,source,created_at,confirmed_at" :: lines) + "\n"
            ctx.Response.ContentType <- "text/csv; charset=utf-8"
            ctx.Response.Headers.Append("Content-Disposition", "attachment; filename=subscribers.csv")
            Response.ofString Encoding.UTF8 csv ctx
