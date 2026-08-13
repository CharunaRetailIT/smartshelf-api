# SmartShelf ESL — Postman collection

Client-facing API collection for the Minew ESL integration. Kept in the repo so it
stays in step with the code it describes.

| File | Purpose |
|---|---|
| `SmartShelf-ESL-API.postman_collection.json` | 31 requests across 7 folders |
| `SmartShelf-ESL.postman_environment.json` | Variables — no secrets committed |

## Getting started

1. Import both files into Postman.
2. Select the **SmartShelf ESL** environment and set `baseUrl`, `userName` and
   `password`. `userName` is the **Employee ID**, not the email address.
3. Run **Auth → Login**. The token, store and user id are captured into the
   environment automatically; every other request picks them up.

Swagger at `/swagger` is generated from the code and covers all 187 endpoints.
This collection is the runnable subset, with the scripting that makes it work
end to end.

## Worth knowing before testing

**No separate sync step.** Creating or updating a product also binds its labels
and pushes the price. There is no publish endpoint to fire afterwards.

**Bulk endpoints are partial-success.** They answer `200` even when rows fail —
read `result.results[]` per row, not the status code alone. `eslPushed` means the
product data reached the cloud; `eslBound` means the label is displaying it.

**Scheduling times are UTC.** Sri Lanka is UTC+5:30, so a 3:00pm local promotion
is `09:30` UTC. The three scheduling *create* requests fill the window in for you
via a pre-request script — copy that script if you build your own client.

**Templates are per store and per screen size.** A template id from another Minew
store is accepted locally and then rejected at bind time with `模板不存在`.

## Two prerequisites for a price to reach a label

1. The store is linked to a Minew store (`StoreMaster.MinewStoreId`). Without it,
   products save to the database only.
2. A label is bound to the product with a template. Send `eslAssignments` on
   create or update and this happens automatically.
