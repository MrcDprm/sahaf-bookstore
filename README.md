# Sahaf

**English** | [Türkçe](README.tr.md)

An online bookshop built with ASP.NET Core MVC. Sahaf sells public-domain classics in its own cover series: customers browse the catalogue, fill a cart and pay with Stripe, schools order in bulk and pay later, and staff run the shop from an admin panel. Every cover is drawn by the app itself.

**Live demo:** [sahaf.miracdeprem.com](https://sahaf.miracdeprem.com). Payments run in Stripe test mode, so use the card `4242 4242 4242 4242` with any future date and any CVC. The site runs on a free server, so the first page can take about half a minute after a quiet period.

![Sahaf home page in Turkish: headline, bookshelf of generated spines and shelf categories](docs/screenshot-home.png)

## Features

**Shop**
- Catalogue of 64 classics in 8 categories, with search by title or author, category filters, five sort orders and paging
- Book pages with bulk price tiers (1-50, 51-100, 100+ copies) and related books
- Generated covers: every book gets its own palette and pattern, so no image files or cover rights are needed
- Cart with quantity changes, bulk prices applied automatically and a receipt-style summary
- Checkout with Stripe; the order is confirmed by a signed webhook and by a check on the return page
- Organisation accounts (e.g. schools) order at once and pay within 30 days, with a "Pay now" button later
- Order history with a status timeline, and e-mails when an order is paid or shipped

**Accounts**
- Sign-up with e-mail confirmation, sign-in with lockout after five failed attempts, password reset
- Profile with a saved delivery address that fills in the checkout form
- Changing the password signs out other devices; the account can be deleted

**Admin panel**
- Books, categories and organisations: create, edit, delete (blocked when other records depend on them)
- Image upload with type, size and content checks; choose a cover or fall back to the generated one
- Orders: start processing, ship with carrier and tracking number, cancel and refund through Stripe
- Users: lock and unlock, change role (customer, organisation, employee, admin); changes apply at once
- Employees manage orders only; admins manage everything

**Everywhere**
- Turkish and English interface, light "paper and ink" theme and dark "reading lamp" theme
- Works on phones; menus and forms also work without JavaScript

## Screenshots

| | |
|---|---|
| ![Dark theme home page in English](docs/screenshot-home-dark.png) | ![Turkish Classics shelf in the catalogue](docs/screenshot-catalog.png) |
| ![Book page with bulk price tiers in the dark theme](docs/screenshot-details.png) | ![Cart with a bulk price applied and the receipt summary](docs/screenshot-cart.png) |
| ![Admin order page with the status timeline and delivery form](docs/screenshot-admin-order.png) | ![Admin book list in the dark theme](docs/screenshot-admin-books.png) |

<p align="center"><img src="docs/screenshot-mobile.png" width="300" alt="Home page on a phone in the dark theme"></p>

## Tech Stack

- **C# / ASP.NET Core MVC** with Razor views, areas (Customer, Admin, Identity) and tag helpers
- **Entity Framework Core** with **PostgreSQL** (Npgsql), repository and unit of work layers
- **ASP.NET Core Identity** for accounts, roles, lockout and tokens
- **Stripe Checkout** and webhooks for payments, **Resend** for e-mail
- Plain CSS and a little JavaScript, no front-end framework
- **xUnit** for tests
- **Docker** on **Render**, database on **Neon**

## Installation

Open [sahaf.miracdeprem.com](https://sahaf.miracdeprem.com), or run it locally. You need the current .NET SDK and PostgreSQL.

```bash
git clone https://github.com/MrcDprm/sahaf-bookstore.git
cd sahaf-bookstore
```

The development connection string in `src/Sahaf.Web/appsettings.Development.json` points to `localhost:5432` with the user `postgres` and the password `postgres`; change it if your database differs. Then set the first admin account and start the app (PowerShell):

```powershell
$env:Admin__Email = "admin@example.com"
$env:Admin__Password = "ChooseAStrongPassw0rd"
dotnet run --project src/Sahaf.Web
```

On the first start the app creates the tables, the roles, the admin account and the 64 books. Open `http://localhost:5080`. Without a Resend key, e-mails are written to the console, so you can open the confirmation link from there. Payments need a Stripe test key in `Stripe__SecretKey` (and `Stripe__WebhookSecret` for the webhook). All settings are listed in [.env.example](.env.example).

Run the tests with:

```bash
dotnet test
```

### Project structure

```
src/
  Sahaf.Models/       Entities and view models
  Sahaf.DataAccess/   DbContext, migrations, repositories, seed data
  Sahaf.Utility/      Pricing, order workflow, cover generator, image checks, e-mail
  Sahaf.Web/          Controllers, views, localization, Stripe, security settings
tests/
  Sahaf.Tests/        Unit tests
Dockerfile            Container used on Render
render.yaml           Render service definition
```

### Security

- Passwords are hashed by Identity; at least 10 characters with upper case, lower case and a digit
- E-mail must be confirmed before signing in; five wrong passwords lock the account for 15 minutes
- Sign-in, sign-up and password forms are rate limited; every form is protected against CSRF
- Forms only copy the fields they allow, so prices, owners and statuses cannot be posted
- Users can only reach their own cart rows and orders; order, cart and image ids are not guessable
- Uploaded images are checked by their first bytes and size, and served with `nosniff`
- The Stripe webhook rejects any request without a valid signature
- Content Security Policy, HSTS and other security headers; no CORS
- Secrets live only in environment variables; production shows friendly error pages, never stack traces

## What I Learned

- **Turning a course project into a product.** Sahaf started as the bookshop I built while following a Udemy ASP.NET Core MVC course. I kept its layered design (models, data access, utilities, web) and the repository and unit of work pattern, then rebuilt the rest: a new database, payments, order management, two languages and a new design.
- **Overposting.** Model binding fills any property it finds in the request. A customer could post an order total of 1 or an admin could post a new creation date. I now bind only what a form needs and copy allowed fields onto the stored entity, and I wrote tests that try to post the protected fields.
- **Checking ownership, not just sign-in.** `[Authorize]` only tells me who the user is. The course version let anyone change another user's cart by changing an id in the URL. Every cart and order action now looks the record up together with the user's id, and those actions only accept POST requests.
- **Payments I can trust.** The browser's return from Stripe can be faked or never happen, so the order is also confirmed by Stripe's webhook. I verify the webhook signature, match the session to the order, and made the "mark as paid" step safe to run twice.
- **When a role change actually applies.** Roles are stored in the sign-in cookie, which Identity re-checks only every 30 minutes by default. A locked user could keep using the site. Checking the security stamp on every request made locks and role changes immediate.
- **Localization without resource files.** I wrote a small `IStringLocalizer` that reads two JSON files. It also translates validation messages and field names, and a test makes sure both languages have the same keys and placeholders.
- **Hosting .NET for free.** Vercel cannot run .NET, so I packaged the app in Docker for Render and used Neon for PostgreSQL. I learned to handle a proxy in front of the app (forwarded headers, HTTPS on port 443), keep cookie keys in the database so sign-ins survive deploys, and keep images in the database because the server's disk is wiped.

## Future Plans

- Stock tracking and out-of-stock books
- Reviews and ratings from customers who bought the book
- Wish list and "notify me" for discounts
- Changing the account e-mail and two-factor sign-in
- Encrypting the cookie keys stored in the database
- Sales reports in the admin panel

## Credits

- Book texts are public-domain classics; descriptions, covers and prices are made up for this demo shop.
- The quote on the home page is the first line of *Pride and Prejudice* by Jane Austen (1813).

## License

[MIT](LICENSE)
