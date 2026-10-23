module ChiAha.ProductSite.Db

open System
open System.IO
open System.Security.Cryptography
open Microsoft.Data.Sqlite

/// A news subscriber. Status is "pending" (signed up, not yet confirmed),
/// "confirmed" (clicked the email link) or "unsubscribed".
type Subscriber = {
    Email: string
    Name: string
    Status: string
    Source: string
    CreatedAt: string
    ConfirmedAt: string
}

let mutable private connString = ""

let init (dbPath: string) =
    let dir = Path.GetDirectoryName(dbPath)
    if dir <> "" && not (Directory.Exists dir) then
        Directory.CreateDirectory(dir) |> ignore
    connString <- sprintf "Data Source=%s" dbPath
    use conn = new SqliteConnection(connString)
    conn.Open()
    use cmd = conn.CreateCommand()
    cmd.CommandText <- """
        CREATE TABLE IF NOT EXISTS subscribers (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            email TEXT NOT NULL UNIQUE COLLATE NOCASE,
            name TEXT NOT NULL DEFAULT '',
            token TEXT NOT NULL UNIQUE,
            status TEXT NOT NULL DEFAULT 'pending',
            source TEXT NOT NULL DEFAULT '',
            created_at TEXT NOT NULL,
            confirmed_at TEXT NOT NULL DEFAULT ''
        );
    """
    cmd.ExecuteNonQuery() |> ignore
    printfn "[Db] Initialized at %s" dbPath

let private openConn () =
    let c = new SqliteConnection(connString)
    c.Open()
    c

let private now () = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")

let private newToken () =
    Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant()

/// Add or refresh a signup and return (token, status). A confirmed subscriber
/// stays confirmed; an unsubscribed one goes back to pending so they must confirm again.
let upsertPending (email: string) (name: string) (source: string) : string * string =
    use conn = openConn ()
    use cmd = conn.CreateCommand()
    cmd.CommandText <- """
        INSERT INTO subscribers (email, name, token, status, source, created_at)
        VALUES ($email, $name, $token, 'pending', $source, $now)
        ON CONFLICT(email) DO UPDATE SET
            name = CASE WHEN excluded.name <> '' THEN excluded.name ELSE subscribers.name END,
            status = CASE WHEN subscribers.status = 'confirmed' THEN 'confirmed' ELSE 'pending' END;
        SELECT token, status FROM subscribers WHERE email = $email;
    """
    cmd.Parameters.AddWithValue("$email", email.Trim().ToLowerInvariant()) |> ignore
    cmd.Parameters.AddWithValue("$name", name.Trim()) |> ignore
    cmd.Parameters.AddWithValue("$token", newToken ()) |> ignore
    cmd.Parameters.AddWithValue("$source", source) |> ignore
    cmd.Parameters.AddWithValue("$now", now ()) |> ignore
    use r = cmd.ExecuteReader()
    if r.Read() then (r.GetString(0), r.GetString(1)) else ("", "")

/// Set a subscriber's status by token. Returns their (email, name) if the token matched.
let private setStatus (token: string) (status: string) : (string * string) option =
    use conn = openConn ()
    use cmd = conn.CreateCommand()
    cmd.CommandText <- """
        UPDATE subscribers
        SET status = $status,
            confirmed_at = CASE WHEN $status = 'confirmed' AND confirmed_at = '' THEN $now ELSE confirmed_at END
        WHERE token = $token;
        SELECT email, name FROM subscribers WHERE token = $token;
    """
    cmd.Parameters.AddWithValue("$token", token) |> ignore
    cmd.Parameters.AddWithValue("$status", status) |> ignore
    cmd.Parameters.AddWithValue("$now", now ()) |> ignore
    use r = cmd.ExecuteReader()
    if r.Read() then Some (r.GetString(0), r.GetString(1)) else None

let confirm (token: string) = setStatus token "confirmed"
let unsubscribe (token: string) = setStatus token "unsubscribed"

let listSubscribers () : Subscriber list =
    use conn = openConn ()
    use cmd = conn.CreateCommand()
    cmd.CommandText <- "SELECT email, name, status, source, created_at, confirmed_at FROM subscribers ORDER BY id"
    use r = cmd.ExecuteReader()
    let rows = ResizeArray<Subscriber>()
    while r.Read() do
        rows.Add
            { Email = r.GetString(0)
              Name = r.GetString(1)
              Status = r.GetString(2)
              Source = r.GetString(3)
              CreatedAt = r.GetString(4)
              ConfirmedAt = r.GetString(5) }
    List.ofSeq rows
