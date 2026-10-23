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

/// POST /subscribe — store a pending signup and email a confirmation link.
let subscribeHandler (config: ProductConfig) : HttpHandler =
    fun ctx ->
        task {
            let! form = Request.getForm ctx
            let name = form.GetString("name", "")
            let email = form.GetString("email", "").Trim()
            let source = form.GetString("source", "site")
            let honeypot = form.GetString("website", "")

            if not (String.IsNullOrEmpty honeypot) then
                return! redirectTo "/?news=check#news" ctx
            elif not (isPlausibleEmail email) then
                return! redirectTo "/?news=err#news" ctx
            else
                let token, status = Db.upsertPending email name source
                if status = "confirmed" then
                    return! redirectTo "/?news=already#news" ctx
                else
                    let link = sprintf "%s/subscribe/confirm?t=%s" config.SiteUrl token
                    let unsub = sprintf "%s/unsubscribe?t=%s" config.SiteUrl token
                    let greeting = if String.IsNullOrWhiteSpace name then "Hello," else sprintf "Hello %s," (htmlEncode (name.Trim()))
                    let html =
                        emailShell (
                            sprintf "<p>%s</p><p>Thank you for asking to hear about my book. Please click below to confirm, and I'll let you know when it's coming out.</p><p><a href=\"%s\" style=\"display:inline-block;background:#ffa6a6;color:#000;font-weight:bold;padding:10px 20px;border-radius:4px;text-decoration:none;\">Yes, keep me posted</a></p><p>If you didn't sign up, just ignore this email and you won't hear from me.</p><p>Mary Lou</p><p style=\"font-size:12px;color:#888;\">maryloubates.com &middot; <a href=\"%s\" style=\"color:#888;\">Unsubscribe</a></p>"
                                greeting link unsub)
                    let! result = sendEmail config email config.ContactToEmail "Please confirm: news about Mary Lou Bates's book" html
                    match result with
                    | Ok () -> return! redirectTo "/?news=check#news" ctx
                    | Error err ->
                        eprintfn "[subscribe] confirmation send failed for %s: %s" email err
                        return! redirectTo "/?news=err#news" ctx
        }

/// GET /subscribe/confirm?t=... — confirm a signup and tell Mary Lou.
let confirmHandler (config: ProductConfig) : HttpHandler =
    fun ctx ->
        task {
            let token = (Request.getQuery ctx).GetString("t", "")
            match (if token = "" then None else Db.confirm token) with
            | Some (email, name) ->
                printfn "[subscribe] confirmed %s" email
                let html =
                    emailShell (
                        sprintf "<p><strong>%s</strong> &lt;%s&gt; signed up for news about your book on maryloubates.com.</p>"
                            (htmlEncode (if name = "" then "(no name)" else name)) (htmlEncode email))
                let! result = sendEmail config config.ContactToEmail email "New reader signed up for book news" html
                match result with
                | Error err -> eprintfn "[subscribe] owner notice failed: %s" err
                | Ok () -> ()
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
