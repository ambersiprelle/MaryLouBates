# Mary Lou Bates — Static Website

A static HTML/CSS website recreated from [maryloubates.com](https://maryloubates.com), hosted on GitHub Pages.

**Live site:** https://ambersiprelle.github.io/MaryLouBates/

---

## Status

### Done ✅
- [x] Homepage with full-screen hero, author bio, article cards, Antarctic timeline, contact form
- [x] 2009 American Rose Annual page — *The Gift of Memories* + *Mam-Maw Sarah's Yellow Roses*
- [x] 2015 American Rose Annual page — *Roses Take You Away Down Memory Lane*
- [x] Dark theme matching original: black background, coral/pink accents
- [x] Original fonts: League Spartan (headings) + Poppins (body)
- [x] Hero image downloaded from original site CDN
- [x] Complete Antarctic timeline (1972–1987)
- [x] Facebook social link preserved
- [x] Mobile-responsive navigation
- [x] Contact form (client-side with thank-you message)
- [x] Pushed to GitHub — https://github.com/ambersiprelle/MaryLouBates

### In Progress 🔄
- [ ] Enable GitHub Pages (Settings → Pages → Branch: main → Save)
  - Will be live at: https://ambersiprelle.github.io/MaryLouBates/

---

## Suggested Next Steps

### High Priority

**1. Custom Domain**
Point `maryloubates.com` to GitHub Pages so the site lives at the real domain.
- In GoDaddy, add a CNAME record: `www` → `ambersiprelle.github.io`
- In GitHub repo Settings → Pages → add custom domain: `www.maryloubates.com`
- Enable "Enforce HTTPS"

**2. Real Article Content**
The 2009 and 2015 Rose Annual pages currently contain recreated article text based on the titles and publication context. If Mary Lou has the original published text, replace the body copy in those two HTML files with the real words.

**3. Author Photo**
Add a photo of Mary Lou to the About section on the homepage. Drop an image in the `images/` folder and add an `<img>` tag alongside the bio text.

**4. Book Page**
Mary Lou is actively writing her Antarctic memoir — a dedicated page for the book would be a great addition:
- Working title / byline
- Synopsis / back cover blurb
- "Stay in touch" email signup for book news
- Progress updates or blog-style posts

---

### Medium Priority

**5. Contact Form — Real Email Delivery**
The contact form currently shows a thank-you message but doesn't send emails. Wire it up with [Formspree](https://formspree.io) (free tier, no backend needed):
- Create a free account at formspree.io
- Replace `<form>` action with your Formspree endpoint
- Submissions arrive in your inbox

**6. Email Newsletter Signup**
Add a Mailchimp or ConvertKit signup form so readers can get notified when the book is ready or when new content is posted.

**7. SEO & Social Sharing**
- Add Open Graph meta tags so links shared on Facebook show a preview image and description
- Add `sitemap.xml` and `robots.txt`
- Add structured data (Schema.org `Person` type) for Mary Lou's name, description, and social links

**8. Favicon**
Add a small favicon — a rose icon or stylized "MLB" monogram — so the browser tab looks polished.

---

### Nice to Have

**9. Antarctica Photo Gallery**
A gallery of photos from Mary Lou and Michael's time on the ice would be a powerful addition — both for the story and for reader engagement.

**10. New Zealand / Australia Page**
The bio mentions covering New Zealand and Australia in the book. A page with photos or stories from those travels would complement the Antarctic timeline well.

**11. Press / Publications List**
A simple page listing all published articles (American Rose Annual, New Zealand Rose Annual, Tennessee Rose Bud, KATnips) with dates and descriptions — a writing portfolio page.

**12. Blog / Updates**
Simple dated posts so Mary Lou can share book progress, rose season updates, or Antarctic memories without needing a full CMS. Even hand-written HTML posts work fine for a low-volume author site.

---

## How to Make Updates

1. Edit files locally in `/Users/ambersiprelle/mary-lou-bates/`
2. Commit changes: `git add . && git commit -m "describe your change"`
3. Push to GitHub: `git push`
4. GitHub Pages rebuilds automatically — live within ~1 minute

## Tech Stack
- Plain HTML5, CSS3, vanilla JavaScript — no frameworks or build tools
- Hosted on GitHub Pages (free)
- Fonts: Google Fonts (League Spartan + Poppins)
- 1 image downloaded from original site CDN
