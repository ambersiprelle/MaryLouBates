# Mary Lou Bates — Development Instructions

## Overview
Personal/bio site for Mary Lou Bates — author, rose enthusiast, Antarctic memoir in progress. Static site recreated from maryloubates.com.

**Live:** https://marylou-bates.fly.dev/
**Repo:** ambersiprelle/MaryLouBates

## Key Details
- **Branch:** main
- **Stack:** Pure static HTML/CSS/JS, served by `nginx:alpine`
- **Deploy:** `fly deploy` (direct, no CI)
- **Fly org:** personal
- **Region:** iad
- **Pages:** 4 (index, timeline, 2009-american-rose-annual, 2015-american-rose-annual)
- **Assets:** `wwwroot/` contains all HTML, css/, js/, images/, robots.txt, sitemap.xml

## SEO Status
Full sweep done: robots.txt, sitemap.xml (4 URLs), OG + Twitter tags, canonicals, **Person** JSON-LD on homepage (not Organization — this is a bio site).

## Notes
- Custom domain `maryloubates.com` not yet wired — sitemap/canonical/og:url all point to fly.dev for now
- Contact form is client-side only (no backend wired)
- Rose Annual article pages contain recreated text; original published words not yet substituted
