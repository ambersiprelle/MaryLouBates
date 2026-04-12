# Mary Lou Bates — Static Website

Personal/author site for Mary Lou Bates — rose enthusiast, former Antarctic researcher, memoir in progress. Recreated from [maryloubates.com](https://maryloubates.com) and hosted on Fly.io.

**Live site:** https://marylou-bates.fly.dev/

---

## Tech Stack
- Plain HTML5, CSS3, vanilla JavaScript — no frameworks or build tools
- Served by `nginx:alpine` on Fly.io (personal org, iad region)
- Fonts: Google Fonts (League Spartan + Poppins)

## Structure
```
wwwroot/          all public assets served by nginx
  index.html      homepage — hero, bio, article cards, Antarctic timeline, contact
  timeline.html   full Antarctic timeline (1972–1987)
  2009-american-rose-annual.html
  2015-american-rose-annual.html
  css/, js/, images/
  robots.txt      allows all, points at sitemap
  sitemap.xml     4 URLs, lastmod 2026-04-12
Dockerfile        4-line nginx:alpine copy
fly.toml          Fly.io app config
```

## Deploy
```
fly deploy
```

No CI — manual deploys only.

## SEO
Full sweep complete:
- `robots.txt` + `sitemap.xml` (4 URLs)
- Open Graph + Twitter Card tags on every page
- Canonical URLs on every page
- **Person** JSON-LD on homepage (not Organization — this is a bio site)

## Suggested Next Steps

### High Priority
1. **Custom domain** — point `maryloubates.com` at Fly.io (`fly certs create maryloubates.com`). Then rewrite sitemap/canonical/og:url to the real domain.
2. **Real article content** — 2009 and 2015 Rose Annual pages contain recreated text. Replace with original published words if available.
3. **Author photo** — add a photo of Mary Lou to the About section on the homepage.
4. **Book page** — dedicated page for the Antarctic memoir (synopsis, progress updates, email signup for book news).

### Medium Priority
5. **Contact form backend** — currently client-side only. Wire to Formspree.
6. **Email newsletter signup** — Mailchimp or ConvertKit for book-release notifications.
7. **Favicon** — rose icon or MLB monogram.

### Nice to Have
8. **Antarctica photo gallery**
9. **New Zealand / Australia travel page**
10. **Press / publications list** (American Rose Annual, NZ Rose Annual, Tennessee Rose Bud, KATnips)
11. **Blog / updates** — simple dated posts for book progress, rose season, Antarctic memories
