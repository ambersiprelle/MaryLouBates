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

let private isPlausibleEmail (email: string) =
    let e = email.Trim()
    e.Length <= 254 && e.Contains("@") && e.LastIndexOf('.') > e.IndexOf('@') && not (e.Contains(" "))

let private emailShell (inner: string) =
    sprintf "<div style=\"font-family:Georgia,serif;font-size:16px;line-height:1.6;color:#333;max-width:560px;\">%s</div>" inner

/// The article every auto-reply points readers to.
let private articleLink (config: ProductConfig) =
    sprintf "<p>While you wait, here is one of my published stories from the <em>American Rose Annual</em>:</p><p><a href=\"%s/2009-american-rose-annual.html\" style=\"display:inline-block;background:#ffa6a6;color:#000;font-weight:bold;padding:10px 20px;border-radius:4px;text-decoration:none;\">Read &ldquo;Mam-Maw Sarah&rsquo;s Yellow Roses&rdquo;</a></p>" config.SiteUrl

let private greetingFor (name: string) =
    if String.IsNullOrWhiteSpace name then "Hello," else sprintf "Hello %s," (htmlEncode (name.Trim()))

/// Welcome email sent the moment someone joins the book news list.
let private sendWelcome (config: ProductConfig) (email: string) (name: string) (token: string) =
    task {
        let unsub = sprintf "%s/unsubscribe?t=%s" config.SiteUrl token
        let html =
            emailShell (
                sprintf "<p>%s</p><p>Thank you for signing up. I am writing a memoir about the four seasons I spent in Antarctica with my husband Michael &mdash; he promised me a rose garden if I would follow him to the ice.</p><p>I'll write when there's news about the book: a sneak peek, the cover, and the day you can buy it.</p>%s<p>Mary Lou</p><p style=\"font-size:12px;color:#888;\">You're getting this because you signed up for book news at maryloubates.com. <a href=\"%s\" style=\"color:#888;\">Unsubscribe</a></p>"
                    (greetingFor name) (articleLink config) unsub)
        let! result = sendEmail config email config.ContactToEmail "Thank you for following my book" html
        match result with
        | Error err -> eprintfn "[subscribe] welcome failed for %s: %s" email err
        | Ok () -> ()
    }

/// Auto-reply to someone who sent a message through the contact form.
let private sendContactAutoReply (config: ProductConfig) (email: string) (name: string) =
    task {
        let html =
            emailShell (
                sprintf "<p>%s</p><p>Thank you for writing. Your message reached me, and I'll reply as soon as I can.</p>%s<p>Mary Lou</p><p style=\"font-size:12px;color:#888;\">You're getting this because you sent a message through maryloubates.com.</p>"
                    (greetingFor name) (articleLink config))
        let! result = sendEmail config email config.ContactToEmail "Thank you for your note" html
        match result with
        | Error err -> eprintfn "[contact] auto-reply failed for %s: %s" email err
        | Ok () -> ()
    }

let contactHandler (config: ProductConfig) : HttpHandler =
    fun ctx ->
        task {
            let! form = Request.getForm ctx
            let name = form.GetString("name", "")
            let email = form.GetString("email", "")
            let message = form.GetString("message", "")

            match Spam.blockReason config ctx form message with
            | Some reason ->
                printfn "[spam] dropped contact from %s (%s): %s" (Spam.clientIp ctx) email reason
                return! redirectTo "/?contact=ok#contact" ctx
            | None when String.IsNullOrWhiteSpace email ->
                return! redirectTo "/?contact=err#contact" ctx
            | None ->
                let! result = sendViaResend config name email message
                match result with
                | Ok () ->
                    if isPlausibleEmail email then do! sendContactAutoReply config (email.Trim()) name
                    return! redirectTo "/?contact=ok#contact" ctx
                | Error err ->
                    eprintfn "[contact] send failed: %s" err
                    return! redirectTo "/?contact=err#contact" ctx
        }

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

            match Spam.blockReason config ctx form "" with
            | Some reason ->
                printfn "[spam] dropped signup from %s (%s): %s" (Spam.clientIp ctx) email reason
                return! redirectTo "/?news=confirmed#news" ctx
            | None when not (isPlausibleEmail email) ->
                return! redirectTo "/?news=err#news" ctx
            | None ->
                let isNew, token = Db.subscribe email name source
                if isNew then
                    printfn "[subscribe] added %s (source: %s)" email source
                    do! sendWelcome config email name token
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

/// GET /form-token — signed page-load timestamp the forms must send back (see Spam.fs).
let formTokenHandler (config: ProductConfig) : HttpHandler =
    fun ctx ->
        ctx.Response.Headers.CacheControl <- "no-store"
        Response.ofPlainText (Spam.issueToken config) ctx
