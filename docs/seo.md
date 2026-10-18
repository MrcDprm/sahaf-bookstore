# SEO: Sahaf

Site: https://sahaf.miracdeprem.com (Turkish by default, English with `?lang=en`)

Sahaf is a portfolio demo: payments run in Stripe test mode. The texts say so, and book pages use the `Book` type without an `Offer`, so search results never show a price or stock that cannot really be bought.

## Keywords

| Type | Turkish | English |
|---|---|---|
| General | kitap al, sahaf, online kitapçı, klasik kitaplar | buy books online, bookshop, classic books |
| Specific | dünya klasikleri, Türk klasikleri, rus klasikleri kitapları, klasik roman önerileri | world classics, Turkish classics, Russian classics books, classic novels |
| Per book | "<kitap adı> <yazar>" (ör. "Suç ve Ceza Dostoyevski") | "<title> <author>" |
| Portfolio | ASP.NET Core e-ticaret projesi, Stripe ödeme örneği | ASP.NET Core e-commerce project, Stripe checkout example |

## Where they are used

- `<title>`: home "Sahaf – Klasik kitaplar: dünya ve Türk klasikleri çevrim içi"; book pages "<title> – <author> · Sahaf"; categories by name
- Meta description: site text, catalog and category texts, and the first 155 characters of each book's description
- Canonical + `hreflang` (tr, en, x-default) on every public page; sorting and search results are not separate pages (search results are `noindex`)
- Open Graph and Twitter card; `og:image` is `/og-image.png`, or the uploaded cover on book pages
- JSON-LD: `WebSite` with `SearchAction` on the home page, `Book` (author, ISBN, pages, genre, cover) on book pages
- `/sitemap.xml` (generated from the database: home, catalog, about, every category and book, both languages) and `/robots.txt` (admin, account, cart and language links closed)
- `noindex`: admin, account, cart, error pages and search results

## Google Search Console

The `miracdeprem.com` domain property covers this subdomain. Under **Sitemaps**, submit `https://sahaf.miracdeprem.com/sitemap.xml`.
