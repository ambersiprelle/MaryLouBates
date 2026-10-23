# Mary Lou Bates — Development Instructions

## Overview
Personal/bio site for Mary Lou Bates — author, rose enthusiast, Antarctic memoir in progress. Static site recreated from maryloubates.com.

**Live:** https://maryloubates.com/ (Fly app marylou-bates)
**Repo:** ambersiprelle/MaryLouBates

## Key Details
- **Branch:** main
- **Stack:** F# Falco 5.1 / .NET 8, serving static files from `wwwroot/`, plus `/contact` and the book news signup
- **Deploy:** `fly deploy` (direct, no CI)
- **Fly org:** personal
- **Region:** iad
- **Pages:** 4 (index, timeline, 2009-american-rose-annual, 2015-american-rose-annual)
- **Assets:** `wwwroot/` contains all HTML, css/, js/, images/, robots.txt, sitemap.xml

## SEO Status
Full sweep done: robots.txt, sitemap.xml (4 URLs), OG + Twitter tags, canonicals, **Person** JSON-LD on homepage (not Organization — this is a bio site).

## Notes
- Custom domain `maryloubates.com` is live (Fly certs issued for apex + www); Resend domain verified 10/23 (key + domain in Amber's Resend account); every email sent is listed at resend.com/emails
- Contact form: `POST /contact` → Resend email to CONTACT_TO_EMAIL (marybatesroses@gmail.com)
- **Book news signup** (`#news` on the homepage), single step (Amber 10/23: no confirmation email):
  - `POST /subscribe` adds the reader as confirmed in SQLite (`/data/mlb.db`, Fly volume `mlb_data`) and emails Mary Lou "New reader signed up"
  - `GET /subscribe/confirm?t=` only honours confirm links sent during the brief double opt-in version
  - `GET /unsubscribe?t=` one-click unsubscribe — put this link (per-subscriber token) in every news email
  - `GET /admin/subscribers.csv` — basic auth `admin` / `ADMIN_PASSWORD` secret; 404 until that secret is set
  - Page shows `?news=confirmed|already|unsubscribed|err` messages (js/main.js)
- Personal site: do NOT route signups through the ChiAha gateway proxy / ActiveCampaign
- Rose Annual article pages contain recreated text; original published words not yet substituted
