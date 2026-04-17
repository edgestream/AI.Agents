---
name: tagesschau-news
description: Fetch and summarise current German news from tagesschau.de RSS feeds. Use when the user asks about German news, Nachrichten, tagesschau, ARD news, news from Germany, or requests a specific topic such as Inland, Ausland, Wirtschaft, Wissen, Faktenfinder, or Investigativ.
license: Apache-2.0
compatibility: Requires outbound network access
---

## tagesschau-news

Use this skill when the user wants current German news from tagesschau.de, asks about what is happening in Germany, or explicitly mentions tagesschau, ARD, or German-language news.

### Feed catalogue

Use the feed that best matches the user's request. When no topic is mentioned, default to the **Alle Meldungen** feed.

| Topic | Feed URL |
|---|---|
| Alle Meldungen (default) | `https://www.tagesschau.de/infoservices/alle-meldungen-100~rss2.xml` |
| Startseite | `https://www.tagesschau.de/index~rss2.xml` |
| Inland | `https://www.tagesschau.de/inland/index~rss2.xml` |
| Inland · Innenpolitik | `https://www.tagesschau.de/inland/innenpolitik/index~rss2.xml` |
| Inland · Gesellschaft | `https://www.tagesschau.de/inland/gesellschaft/index~rss2.xml` |
| Inland · Regional (all) | `https://www.tagesschau.de/inland/regional/index~rss2.xml` |
| Inland · Regional · Baden-Württemberg | `https://www.tagesschau.de/inland/regional/badenwuerttemberg/index~rss2.xml` |
| Inland · Regional · Bayern | `https://www.tagesschau.de/inland/regional/bayern/index~rss2.xml` |
| Inland · Regional · Berlin | `https://www.tagesschau.de/inland/regional/berlin/index~rss2.xml` |
| Inland · Regional · Brandenburg | `https://www.tagesschau.de/inland/regional/brandenburg/index~rss2.xml` |
| Inland · Regional · Bremen | `https://www.tagesschau.de/inland/regional/bremen/index~rss2.xml` |
| Inland · Regional · Hamburg | `https://www.tagesschau.de/inland/regional/hamburg/index~rss2.xml` |
| Inland · Regional · Hessen | `https://www.tagesschau.de/inland/regional/hessen/index~rss2.xml` |
| Inland · Regional · Mecklenburg-Vorpommern | `https://www.tagesschau.de/inland/regional/mecklenburgvorpommern/index~rss2.xml` |
| Inland · Regional · Niedersachsen | `https://www.tagesschau.de/inland/regional/niedersachsen/index~rss2.xml` |
| Inland · Regional · Nordrhein-Westfalen | `https://www.tagesschau.de/inland/regional/nordrheinwestfalen/index~rss2.xml` |
| Inland · Regional · Rheinland-Pfalz | `https://www.tagesschau.de/inland/regional/rheinlandpfalz/index~rss2.xml` |
| Inland · Regional · Saarland | `https://www.tagesschau.de/inland/regional/saarland/index~rss2.xml` |
| Inland · Regional · Sachsen | `https://www.tagesschau.de/inland/regional/sachsen/index~rss2.xml` |
| Inland · Regional · Sachsen-Anhalt | `https://www.tagesschau.de/inland/regional/sachsenanhalt/index~rss2.xml` |
| Inland · Regional · Schleswig-Holstein | `https://www.tagesschau.de/inland/regional/schleswigholstein/index~rss2.xml` |
| Inland · Regional · Thüringen | `https://www.tagesschau.de/inland/regional/thueringen/index~rss2.xml` |
| Ausland | `https://www.tagesschau.de/ausland/index~rss2.xml` |
| Ausland · Europa | `https://www.tagesschau.de/ausland/europa/index~rss2.xml` |
| Ausland · Amerika | `https://www.tagesschau.de/ausland/amerika/index~rss2.xml` |
| Ausland · Afrika | `https://www.tagesschau.de/ausland/afrika/index~rss2.xml` |
| Ausland · Asien | `https://www.tagesschau.de/ausland/asien/index~rss2.xml` |
| Ausland · Ozeanien | `https://www.tagesschau.de/ausland/ozeanien/index~rss2.xml` |
| Wirtschaft | `https://www.tagesschau.de/wirtschaft/index~rss2.xml` |
| Wirtschaft · Finanzen | `https://www.tagesschau.de/wirtschaft/finanzen/index~rss2.xml` |
| Wirtschaft · Unternehmen | `https://www.tagesschau.de/wirtschaft/unternehmen/index~rss2.xml` |
| Wirtschaft · Verbraucher | `https://www.tagesschau.de/wirtschaft/verbraucher/index~rss2.xml` |
| Wirtschaft · Technologie | `https://www.tagesschau.de/wirtschaft/technologie/index~rss2.xml` |
| Wirtschaft · Weltwirtschaft | `https://www.tagesschau.de/wirtschaft/weltwirtschaft/index~rss2.xml` |
| Wirtschaft · Konjunktur | `https://www.tagesschau.de/wirtschaft/konjunktur/index~rss2.xml` |
| Wissen | `https://www.tagesschau.de/wissen/index~rss2.xml` |
| Wissen · Gesundheit | `https://www.tagesschau.de/wissen/gesundheit/index~rss2.xml` |
| Wissen · Klima & Umwelt | `https://www.tagesschau.de/wissen/klima/index~rss2.xml` |
| Wissen · Forschung | `https://www.tagesschau.de/wissen/forschung/index~rss2.xml` |
| Wissen · Technologie | `https://www.tagesschau.de/wissen/technologie/index~rss2.xml` |
| Faktenfinder | `https://www.tagesschau.de/faktenfinder/index~rss2.xml` |
| Investigativ | `https://www.tagesschau.de/investigativ/index~rss2.xml` |

### Steps

1. Identify which topic or region the user is asking about. Map it to the most specific matching feed URL from the catalogue above.
2. If the user mentions a German *Bundesland* (federal state), select the corresponding regional feed.
3. Call the `fetch` tool with the chosen feed URL to retrieve the RSS 2.0 XML.
4. Parse the returned XML: for each `<item>` element extract `<title>`, `<description>` (strip HTML tags), `<link>`, and `<pubDate>`.
5. Present the top items as a concise, bulleted news summary. Include the publication date and a link for each item.
6. If the user asked for a specific topic within the results (e.g. "only technology"), filter items by matching keywords in the title or description before presenting them.

### Parsing notes

- The feeds return RSS 2.0 XML. Strip any CDATA wrappers and HTML entities from `<description>` before presenting text.
- `<pubDate>` is in RFC 2822 format (e.g. `Mon, 07 Apr 2026 18:00:00 +0200`). Convert to a readable local format when presenting.
- Limit the default output to the **10 most recent items** unless the user requests more.

### Edge cases

- If the feed returns an HTTP error or empty body, inform the user and suggest trying the **Alle Meldungen** feed as a fallback.
- If the user's topic cannot be mapped to a known feed, fetch **Alle Meldungen** and filter the results by keywords.
- Do not fabricate news items. Only present items explicitly present in the fetched feed.

### Example

User: "Was sind die aktuellen Nachrichten aus Bayern?"

1. Map "Bayern" → `https://www.tagesschau.de/inland/regional/bayern/index~rss2.xml`
2. Call `fetch` with that URL.
3. Parse the `<item>` elements from the XML response.
4. Return a bulleted summary of the 10 most recent Bavarian news items with titles, brief descriptions, dates, and links.
