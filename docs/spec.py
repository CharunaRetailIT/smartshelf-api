# -*- coding: utf-8 -*-
"""
Content model for the SmartShelf ESL Open API document.

Structure mirrors the Minew "ESL Cloud Platform Open API" reference the client
already works from: numbered Parts, a meta table per endpoint, a request
parameter table, a request demo, a return-code table and a sample return.

Ordering is by integration flow rather than by controller, so a reader works
top to bottom: authenticate, fetch reference data, create products, load in
bulk, inspect labels, then schedule.
"""

BRAND = {
    "company": "Retail IT (Pvt) Ltd",
    "product": "SmartShelf ESL",
    "title": "SmartShelf ESL\nOpen API",
    "version": "V1.0.0",
    "website": "retailit.lk",
    "email": "support@retailit.lk",
    "green": "#0b7335",
}

HISTORY = [
    ("V1.0.0", "2026.08.14", "First release. Product, bulk, device and\nscheduling APIs for Minew ESL."),
]

NOTE = [
    "All endpoints are served from <code>https://esl-api.retailit.lk</code>. The full URL is "
    "given with each endpoint and may be used as printed. Requests must be made over HTTPS.",

    "Every request except <b>Log in</b> requires the bearer token returned by "
    "<b>Part 2.1</b>, sent as the header <code>Authorization: Bearer &lt;token&gt;</code>.",

    "Every product operation is scoped to a single store. Product codes are unique "
    "<i>per store</i> and not globally, so <code>storeId</code> is required wherever it appears.",

    "Prices reach the electronic shelf labels as part of the call that changes them. "
    "There is no separate synchronise or publish endpoint to call afterwards.",
]

# ---------------------------------------------------------------------------
# Reusable fragments
# ---------------------------------------------------------------------------

ENVELOPE_NOTE = (
    "Wrapped in the standard response envelope described in <b>Appendix A</b>."
)

CODES_STANDARD = [
    ("200", "Success."),
    ("400", "The request was rejected. <code>message</code> carries the reason."),
    ("401", "Missing, expired or invalid bearer token."),
    ("403", "The signed-in user's role does not permit this operation."),
    ("404", "No matching record."),
    ("500", "Unexpected server error. <code>error</code> carries the exception text."),
]

CODES_READ = [
    ("200", "Success."),
    ("401", "Missing, expired or invalid bearer token."),
    ("404", "No matching record for the supplied identifier and store."),
    ("500", "Unexpected server error."),
]


def p(key, pos, typ, req, example, desc):
    return {"key": key, "pos": pos, "type": typ, "req": req,
            "example": example, "desc": desc}


# ---------------------------------------------------------------------------
# Parts
# ---------------------------------------------------------------------------

PARTS = []

# ------------------------------- Part 1 ------------------------------------
PARTS.append({
    "title": "Before you begin",
    "intro":
        "This part explains the conventions used throughout the document. It contains no "
        "endpoints. Read it once before working through the parts that follow.",
    "prose": [
        ("Base address",
         "Every endpoint in this document is served from <code>https://esl-api.retailit.lk</code>. "
         "URLs are printed in full and require no substitution. All traffic is over HTTPS; plain "
         "HTTP requests are not accepted."),
        ("Authentication",
         "SmartShelf uses bearer-token authentication. Call <b>Part 2.1 Log in</b> once, then send "
         "the returned token on every subsequent request as the header "
         "<code>Authorization: Bearer &lt;token&gt;</code>. The token also identifies the store the "
         "signed-in user belongs to."),
        ("Roles",
         "Read operations are available to every signed-in user. Operations that create, change or "
         "delete data require the <b>Admin</b>, <b>Manager</b> or <b>Operator</b> role; a user with "
         "the <b>Viewer</b> role receives <code>403</code>. Where an endpoint is more restricted "
         "than this, its description says so."),
        ("Response envelope",
         "Every endpoint returns the same envelope. Read <code>success</code> first; the payload is "
         "always under <code>result</code>. The full shape is given in <b>Appendix A</b>."),
        ("Two conditions for a price to reach a label",
         "First, the store must be linked to a Minew cloud store. Second, a label must be bound to "
         "the product with a template. Sending <code>eslAssignments</code> when creating or updating "
         "a product satisfies the second condition automatically. <b>Appendix C</b> covers what "
         "happens when either is missing."),
    ],
    "endpoints": [],
})

# ------------------------------- Part 2 ------------------------------------
PARTS.append({
    "title": "Authentication",
    "intro":
        "Obtain the bearer token used by every other endpoint in this document.",
    "endpoints": [{
        "name": "Log in",
        "url": "https://esl-api.retailit.lk/api/auth/login",
        "method": "POST",
        "content_type": "application/json;charset=utf-8",
        "auth": "None. This is the only endpoint that does not require a token.",
        "desc":
            "Exchanges an employee identifier and password for a bearer token. The response also "
            "carries the user's store, which supplies the <code>storeId</code> used throughout the "
            "rest of this document.",
        "notes": [
            "<code>userName</code> is the <b>Employee ID</b>, not the user's email address. "
            "Supplying the email address returns <code>401</code>.",
        ],
        "params": [
            p("userName", "Body", "String", "Yes", '"ADMIN01"', "Employee identifier."),
            p("password", "Body", "String", "Yes", '"••••••••"', "Account password, sent in plain text over TLS."),
        ],
        "demo": '''{
  "userName": "ADMIN01",
  "password": "••••••••"
}''',
        "codes": [
            ("200", "Success. A token is returned."),
            ("401", "Invalid credentials, or the employee identifier does not exist."),
            ("500", "Unexpected server error."),
        ],
        "returns": '''{
  "success": true,
  "message": "Login successful",
  "responsCode": 200,
  "result": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "user": {
      "id": 42,
      "employeeId": "ADMIN01",
      "firstName": "Nimal",
      "lastName": "Perera",
      "email": "admin01@retailit.lk",
      "storeId": 3,
      "store": {
        "id": 3,
        "storeName": "Colombo City Centre",
        "storeCode": "CCC-01",
        "minewStoreId": "2087891877587062784"
      }
    }
  }
}''',
    }],
})

# ------------------------------- Part 3 ------------------------------------
PARTS.append({
    "title": "Reference data",
    "intro":
        "Read-only lookups that supply the identifiers used when building product and label "
        "payloads. Call these once and cache the results; they change rarely.",
    "endpoints": [
        {
            "name": "Query category list",
            "url": "https://esl-api.retailit.lk/api/products/all-categories",
            "method": "GET",
            "content_type": "—",
            "auth": "Bearer token.",
            "desc":
                "Product categories defined for a store. Supplies the values used as "
                "<code>categoryId</code>, or as <code>categoryName</code> in bulk payloads.",
            "params": [
                p("storeId", "Query", "Long", "Yes", "3", "Store to list categories for."),
                p("pageNumber", "Query", "Int", "No", "1", "Page number. Defaults to 1."),
                p("pageSize", "Query", "Int", "No", "100", "Rows per page. Defaults to 10, maximum 100."),
                p("searchTerm", "Query", "String", "No", '"Bev"', "Filters on category name."),
            ],
            "demo": "GET /api/products/all-categories?storeId=3&pageNumber=1&pageSize=100",
            "codes": CODES_READ,
            "returns": '''{
  "success": true,
  "message": "Categories retrieved successfully.",
  "responsCode": 200,
  "result": {
    "items": [
      { "id": 3, "categoryCode": "BEV", "categoryName": "Beverages", "isActive": true, "storeId": 3 },
      { "id": 4, "categoryCode": "BAK", "categoryName": "Bakery",    "isActive": true, "storeId": 3 }
    ],
    "totalCount": 2,
    "pageNumber": 1,
    "pageSize": 100,
    "totalPages": 1
  }
}''',
        },
        {
            "name": "Query sub-category list",
            "url": "https://esl-api.retailit.lk/api/products/active-subcategories",
            "method": "GET",
            "content_type": "—",
            "auth": "Bearer token.",
            "desc":
                "Active sub-categories, optionally narrowed to one category. A sub-category is "
                "always subordinate to a category; supplying one that belongs to a different "
                "category is rejected when the product is saved.",
            "params": [
                p("storeId", "Query", "Long", "Yes", "3", "Store to list sub-categories for."),
                p("categoryId", "Query", "Long", "No", "3", "Restricts the result to one category."),
                p("pageNumber", "Query", "Int", "No", "1", "Page number."),
                p("pageSize", "Query", "Int", "No", "100", "Rows per page."),
            ],
            "demo": "GET /api/products/active-subcategories?storeId=3&categoryId=3",
            "codes": CODES_READ,
            "returns": '''{
  "success": true,
  "message": "Sub-categories retrieved successfully.",
  "responsCode": 200,
  "result": {
    "items": [
      { "id": 4, "subCategoryName": "Tea & Coffee", "categoryId": 3, "isActive": true, "storeId": 3 }
    ],
    "totalCount": 1,
    "pageNumber": 1,
    "pageSize": 100,
    "totalPages": 1
  }
}''',
        },
        {
            "name": "Query label list",
            "url": "https://esl-api.retailit.lk/api/device/devices/local",
            "method": "GET",
            "content_type": "—",
            "auth": "Bearer token.",
            "desc":
                "Every active electronic shelf label held by SmartShelf. The <code>id</code> values "
                "returned here are used as <code>deviceId</code> when binding a product to a label.",
            "params": [],
            "demo": "GET /api/device/devices/local",
            "codes": CODES_READ,
            "returns": '''{
  "success": true,
  "message": "Devices retrieved successfully.",
  "responsCode": 200,
  "result": [
    {
      "id": 10,
      "mac": "e0000000be65",
      "deviceName": "Aisle 1 - Shelf 2",
      "deviceType": "Minew",
      "screenInch": 4.20,
      "screenColor": "bwry",
      "battery": 100,
      "isOnline": true,
      "storeId": 3
    }
  ]
}''',
        },
        {
            "name": "Query template list",
            "url": "https://esl-api.retailit.lk/api/device/template/local",
            "method": "GET",
            "content_type": "—",
            "auth": "Bearer token.",
            "desc":
                "Label templates synchronised from the Minew console. A template determines what the "
                "label draws — price, name, barcode and so on.",
            "notes": [
                "Template identifiers are long numeric <b>strings</b>. Keep them quoted in JSON; "
                "parsing one as a number loses precision and produces an identifier that does not exist.",
                "A template belongs to one Minew store and one screen size. Using a template from "
                "another store, or one whose size does not match the label, is accepted by SmartShelf "
                "and then rejected by the cloud when the label is bound.",
                "Pass <code>storeId</code> to list only that store's templates. Omitted, the call "
                "returns the templates of every store, which is rarely what an integration wants — "
                "a template from another store is refused by the cloud at bind time.",
            ],
            "params": [
                p("storeId", "Query", "Long", "No", "3", "Restricts the list to one store. Strongly recommended."),
            ],
            "demo": "GET /api/device/template/local?storeId=3",
            "codes": CODES_READ,
            "returns": '''{
  "success": true,
  "message": "Templates retrieved successfully.",
  "responsCode": 200,
  "result": [
    {
      "id": "2087930462289793024",
      "name": "RIT 4.2 Dynamic",
      "screenInch": 4.20,
      "screenWidth": 400,
      "screenHeight": 300,
      "color": "bwry",
      "storeId": 3,
      "isActive": true
    }
  ]
}''',
        },
        {
            "name": "Query a template",
            "url": "https://esl-api.retailit.lk/api/device/template/{id}",
            "method": "GET",
            "content_type": "—",
            "auth": "Bearer token.",
            "desc":
                "One template, including the panel size it was designed for. Use it to confirm a "
                "template belongs to the store and fits the label before binding.",
            "notes": [
                "Pass <code>storeId</code>. A template belongs to one store in the Minew cloud, "
                "and binding one from another store fails at the cloud with <i>template does not "
                "exist</i>. With <code>storeId</code> supplied, a template outside that store "
                "answers <code>404</code> here instead — a much clearer failure.",
                "Template identifiers are long numeric strings. Keep them quoted; parsing one as "
                "a number loses precision.",
            ],
            "params": [
                p("id", "Path", "String", "Yes", '"2087930462289793024"', "Template identifier."),
                p("storeId", "Query", "Long", "No", "3", "Store the template must belong to. Strongly recommended."),
            ],
            "demo": "GET /api/device/template/2087930462289793024?storeId=3",
            "codes": [
                ["200", "Template returned."],
                ["404", "No such template, or it belongs to another store."],
                ["500", "Unexpected server error."],
            ],
            "returns": '''{
  "success": true,
  "message": "Template retrieved successfully.",
  "responsCode": 200,
  "result": {
    "id": "2087930462289793024",
    "name": "RIT 4.2 Dynamic",
    "description": "",
    "screenInch": 4.20,
    "screenWidth": 400,
    "screenHeight": 300,
    "color": "bwry",
    "orientation": 0,
    "storeId": 3,
    "isActive": true
  }
}''',
        },
        {
            "name": "Preview a template",
            "url": "https://esl-api.retailit.lk/api/device/templates/preview",
            "method": "POST",
            "content_type": "application/json;charset=utf-8",
            "auth": "Bearer token.",
            "desc":
                "Renders a template as an image, so it can be shown to an operator before it is "
                "sent to a label. Returns the picture as a base64 bitmap.",
            "notes": [
                "Set <code>isBound</code> to <code>false</code> to render the template as "
                "designed. Set it to <code>true</code>, with <code>mac</code>, to render what a "
                "particular label is currently displaying — that variant returns nothing unless "
                "the label is bound to this template.",
                "<code>storeId</code> accepts either the SmartShelf store id or the Minew cloud "
                "id. A template belonging to a different store is refused with <code>400</code> "
                "rather than passed to the cloud.",
                "The reply is <b>not</b> the standard envelope: it is a single "
                "<code>data</code> field holding the image as a base64 string, or "
                "<code>null</code> when the cloud has no preview to give. The image is large — "
                "commonly several hundred kilobytes.",
            ],
            "params": [
                p("templateId", "Body", "String", "Yes", '"2087930462289793024"', "Template to render."),
                p("isBound", "Body", "Boolean", "No", "false", "False renders the design; true renders what a label shows."),
                p("mac", "Body", "String", "No*", '"e0000000be65"', "* Required when <code>isBound</code> is true."),
                p("storeId", "Body", "String", "No", '"3"', "Store the template must belong to. Quoted."),
            ],
            "demo": '''{
  "templateId": "2087930462289793024",
  "isBound": false,
  "storeId": "3"
}''',
            "codes": [
                ["200", "Rendered. Read <code>data</code>."],
                ["400", "<code>templateId</code> missing, or the template belongs to another store."],
                ["404", "No such template."],
                ["500", "Unexpected server error."],
            ],
            "returns": '''{
  "data": "Qk12fgUAAAAAADYAAAAoAAAAkAEAAFwBAAABABgAAAAAAEB+BQA..."
}''',
        },
    ],
})

# ------------------------------- Part 4 ------------------------------------
PARTS.append({
    "title": "Product API",
    "intro":
        "Create, amend and retrieve individual products. A product may be created on its own, or "
        "together with the labels that display it. Where labels are supplied, the price is pushed "
        "and the labels are bound as part of the same call.",
    "endpoints": [
        {
            "name": "Add product",
            "url": "https://esl-api.retailit.lk/api/products/product",
            "method": "POST",
            "content_type": "application/json;charset=utf-8",
            "auth": "Bearer token. Admin, Manager or Operator.",
            "desc":
                "Creates a product with no label attached. Use this when the product belongs in the "
                "catalogue but has not yet been placed on a shelf.",
            "notes": [
                "Send <code>subCategoryId: 0</code> when the product has no sub-category.",
                "<code>productCode</code> must be unique within the store.",
            ],
            "params": [
                p("productCode", "Body", "String", "Yes", '"PRD-1001"', "Unique product code within the store."),
                p("productName", "Body", "String", "Yes", '"Ceylon Black Tea 200g"', "Display name."),
                p("barCode", "Body", "String", "No", '"4791234500011"', "Barcode printed on the label."),
                p("categoryId", "Body", "Long", "Yes", "3", "From Part 3.1."),
                p("subCategoryId", "Body", "Long", "No", "4", "From Part 3.2. Send 0 when not applicable."),
                p("quantity", "Body", "Decimal", "No", "120", "Stock quantity held on the product record."),
                p("unitOfMeasure", "Body", "String", "No", '"Pack"', "Unit shown alongside the quantity."),
                p("costPrice", "Body", "Decimal", "No", "780.00", "Cost price."),
                p("sellingPrice", "Body", "Decimal", "Yes", "1050.00", "Shelf price. This is the value rendered on the label."),
                p("discountPrice", "Body", "Decimal", "No", "105.00", "Discount amount."),
                p("discountedPrice", "Body", "Decimal", "No", "945.00", "Price after discount."),
                p("discountPercentage", "Body", "Decimal", "No", "10.00", "Discount percentage."),
                p("wholesalePrice", "Body", "Decimal", "No", "900.00", "Wholesale price."),
                p("minimumPrice", "Body", "Decimal", "No", "850.00", "Lowest permitted selling price."),
                p("maximumPrice", "Body", "Decimal", "No", "1200.00", "Highest permitted selling price."),
                p("description", "Body", "String", "No", '"Pure Ceylon tea"', "Free-text description."),
                p("isActive", "Body", "Boolean", "No", "true", "Defaults to true."),
                p("createdUser", "Body", "Int", "Yes", "42", "Identifier of the user making the change."),
                p("storeId", "Body", "Long", "Yes", "3", "Store the product belongs to."),
            ],
            "demo": '''{
  "productCode": "PRD-1001",
  "productName": "Ceylon Black Tea 200g",
  "barCode": "4791234500011",
  "categoryId": 3,
  "subCategoryId": 4,
  "quantity": 120,
  "unitOfMeasure": "Pack",
  "costPrice": 780.00,
  "sellingPrice": 1050.00,
  "discountPrice": 105.00,
  "discountedPrice": 945.00,
  "discountPercentage": 10.00,
  "wholesalePrice": 900.00,
  "minimumPrice": 850.00,
  "maximumPrice": 1200.00,
  "description": "Pure Ceylon black tea leaves",
  "isActive": true,
  "createdUser": 42,
  "storeId": 3
}''',
            "codes": [
                ("201", "Created."),
                ("400", "A product with the same code already exists, or the category is invalid."),
                ("403", "The user's role does not permit creating products."),
                ("500", "Unexpected server error."),
            ],
            "returns": '''{
  "success": true,
  "message": "Product created successfully.",
  "responsCode": 201,
  "result": {
    "id": 6,
    "productCode": "PRD-1001",
    "productName": "Ceylon Black Tea 200g",
    "sellingPrice": 1050.00,
    "storeId": 3,
    "createdDate": "2026-08-14T09:30:00"
  }
}''',
        },
        {
            "name": "Add product with labels",
            "url": "https://esl-api.retailit.lk/api/products/with-esl",
            "method": "POST",
            "content_type": "application/json;charset=utf-8",
            "auth": "Bearer token. Admin, Manager or Operator.",
            "desc":
                "Creates a product together with the labels that display it, in a single database "
                "transaction. If any part fails nothing is saved, so a product cannot be left "
                "half-configured with a label pointing at nothing.",
            "notes": [
                "This one call puts the product on the label. The assignment is saved, the price is "
                "pushed and the label is bound, so it begins displaying immediately.",
                "Send <code>\"bindToEsl\": false</code> to record the assignment without contacting "
                "the cloud. The label will not display the product until it is bound.",
                "Send <code>\"eslAssignments\": []</code> to create the product without any label.",
            ],
            "params": [
                p("product", "Body", "Object", "Yes", "—", "Product fields, as in Part 4.1 but without <code>createdUser</code>."),
                p("product.storeId", "Body", "Long", "Yes", "3", "Store the product belongs to."),
                p("eslAssignments", "Body", "Array", "No", "—", "One entry per label. Omit or send an empty array for no label."),
                p("&nbsp;&nbsp;deviceId", "Body", "Long", "Yes", "10", "Label identifier, from Part 3.3."),
                p("&nbsp;&nbsp;templateId", "Body", "String", "Yes", '"2087930462289793024"', "Template identifier, from Part 3.4. Keep quoted."),
                p("&nbsp;&nbsp;messageId", "Body", "Long", "No", "null", "Optional promotional message rendered with the template."),
                p("&nbsp;&nbsp;displayOrder", "Body", "Int", "No", "1", "Order when several labels serve one product."),
                p("&nbsp;&nbsp;isActive", "Body", "Boolean", "No", "true", "Defaults to true."),
                p("userId", "Body", "Int", "Yes", "42", "Identifier of the user making the change."),
                p("bindToEsl", "Body", "Boolean", "No", "true", "Bind the labels in the cloud. Defaults to true."),
            ],
            "demo": '''{
  "product": {
    "productCode": "PRD-1002",
    "productName": "Ground Coffee 250g",
    "barCode": "4791234500028",
    "categoryId": 3,
    "subCategoryId": 4,
    "quantity": 60,
    "unitOfMeasure": "Pack",
    "costPrice": 1200.00,
    "sellingPrice": 1690.00,
    "discountPrice": 90.00,
    "discountedPrice": 1600.00,
    "isActive": true,
    "storeId": 3
  },
  "eslAssignments": [
    {
      "deviceId": 10,
      "templateId": "2087930462289793024",
      "messageId": null,
      "displayOrder": 1,
      "isActive": true
    }
  ],
  "userId": 42,
  "bindToEsl": true
}''',
            "codes": [
                ("201", "Created. The message states how many labels were bound."),
                ("400", "Duplicate product code, invalid category, or an unknown label or template."),
                ("403", "The user's role does not permit creating products."),
                ("500", "Unexpected server error. Nothing was saved."),
            ],
            "returns": '''{
  "success": true,
  "message": "Product and ESL assignments created successfully. 1 label(s) bound.",
  "responsCode": 201,
  "result": {
    "id": 7,
    "productCode": "PRD-1002",
    "productName": "Ground Coffee 250g",
    "sellingPrice": 1690.00,
    "storeId": 3,
    "createdDate": "2026-08-14T09:35:00"
  }
}''',
        },
        {
            "name": "Modify product",
            "url": "https://esl-api.retailit.lk/api/products/product/{id}",
            "method": "PUT",
            "content_type": "application/json;charset=utf-8",
            "auth": "Bearer token. Admin, Manager or Operator.",
            "desc":
                "Replaces the product's fields and pushes the new price to any label already bound "
                "to it.",
            "notes": [
                "This endpoint <b>replaces</b> the record. Any field omitted from the request is "
                "overwritten with its default. For a price-only feed use <b>Part 5.2</b>, which "
                "changes only the fields supplied.",
            ],
            "params": [
                p("id", "Path", "Long", "Yes", "6", "Product identifier."),
                p("…", "Body", "—", "—", "—", "Same fields as Part 4.1, with <code>updatedUser</code> in place of <code>createdUser</code>."),
            ],
            "demo": '''{
  "productCode": "PRD-1001",
  "productName": "Ceylon Black Tea 200g",
  "barCode": "4791234500011",
  "categoryId": 3,
  "subCategoryId": 4,
  "quantity": 118,
  "unitOfMeasure": "Pack",
  "costPrice": 780.00,
  "sellingPrice": 1099.00,
  "discountPrice": 99.00,
  "discountedPrice": 1000.00,
  "description": "Pure Ceylon black tea leaves",
  "isActive": true,
  "updatedUser": 42,
  "storeId": 3
}''',
            "codes": CODES_STANDARD,
            "returns": '''{
  "success": true,
  "message": "Product updated successfully.",
  "responsCode": 200,
  "result": {
    "id": 6,
    "productCode": "PRD-1001",
    "sellingPrice": 1099.00,
    "updatedDate": "2026-08-14T10:02:00"
  }
}''',
        },
        {
            "name": "Modify product with labels",
            "url": "https://esl-api.retailit.lk/api/products/{id}/with-esl",
            "method": "PUT",
            "content_type": "application/json;charset=utf-8",
            "auth": "Bearer token. Admin, Manager or Operator.",
            "desc":
                "Updates the product and reconciles the labels bound to it, in a single transaction. "
                "The request body is the same shape as <b>Part 4.2</b>.",
            "notes": [
                "To <b>remove</b> a label, send its row with <code>\"isDeleted\": true</code> together "
                "with the <code>templateAssignmentId</code> returned by <b>Part 4.6</b>. Removing an "
                "assignment also unbinds the label in the cloud, so it stops showing the old price.",
                "Labels present in the request that are not already bound are bound as part of this call.",
            ],
            "params": [
                p("id", "Path", "Long", "Yes", "7", "Product identifier."),
                p("product", "Body", "Object", "Yes", "—", "Product fields, as in Part 4.2."),
                p("eslAssignments", "Body", "Array", "No", "—", "Desired label state after the call."),
                p("&nbsp;&nbsp;templateAssignmentId", "Body", "Long", "No", "9", "Identifier of an existing binding, required when removing it."),
                p("&nbsp;&nbsp;isDeleted", "Body", "Boolean", "No", "false", "Set true to unbind this label."),
                p("userId", "Body", "Int", "Yes", "42", "Identifier of the user making the change."),
            ],
            "demo": '''{
  "product": {
    "productCode": "PRD-1002",
    "productName": "Ground Coffee 250g",
    "categoryId": 3,
    "sellingPrice": 1750.00,
    "discountPrice": 150.00,
    "isActive": true,
    "storeId": 3
  },
  "eslAssignments": [
    {
      "deviceId": 10,
      "templateId": "2087930462289793024",
      "templateAssignmentId": 9,
      "displayOrder": 1,
      "isActive": true,
      "isDeleted": false
    }
  ],
  "userId": 42
}''',
            "codes": CODES_STANDARD,
            "returns": '''{
  "success": true,
  "message": "Product and ESL assignments updated successfully. 1 label(s) bound.",
  "responsCode": 200,
  "result": {
    "id": 7,
    "productCode": "PRD-1002",
    "sellingPrice": 1750.00,
    "updatedDate": "2026-08-14T10:20:00"
  }
}''',
        },
        {
            "name": "Query product list",
            "url": "https://esl-api.retailit.lk/api/products",
            "method": "GET",
            "content_type": "—",
            "auth": "Bearer token.",
            "desc":
                "Paged product list for one store. Returns product fields only, without label "
                "information. Use <b>Part 4.6</b> or <b>Part 4.7</b> when label detail is needed.",
            "notes": [
                "When nothing matches, the response is <code>404</code> with "
                "<code>totalCount: 0</code> rather than an empty <code>200</code>.",
            ],
            "params": [
                p("storeId", "Query", "Long", "Yes", "3", "Store to list products for."),
                p("pageNumber", "Query", "Int", "No", "1", "Page number. Defaults to 1."),
                p("pageSize", "Query", "Int", "No", "20", "Rows per page. Defaults to 10."),
                p("categoryId", "Query", "Long", "No", "3", "Restricts to one category."),
                p("subcategoryId", "Query", "Long", "No", "4", "Restricts to one sub-category."),
                p("searchTerm", "Query", "String", "No", '"tea"', "Matches product code, name and barcode."),
            ],
            "demo": "GET /api/products?storeId=3&pageNumber=1&pageSize=20&searchTerm=tea",
            "codes": [
                ("200", "Success."),
                ("401", "Missing, expired or invalid bearer token."),
                ("404", "No products matched."),
                ("500", "Unexpected server error."),
            ],
            "returns": '''{
  "success": true,
  "message": "Products retrieved successfully.",
  "responsCode": 200,
  "result": {
    "items": [
      {
        "id": 6,
        "productCode": "PRD-1001",
        "productName": "Ceylon Black Tea 200g",
        "categoryName": "Beverages",
        "subCategoryName": "Tea & Coffee",
        "sellingPrice": 1050.00,
        "discountPrice": 105.00,
        "isActive": true,
        "storeId": 3
      }
    ],
    "totalCount": 1,
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 1,
    "searchTerm": "tea"
  }
}''',
        },
        {
            "name": "Query product by product code",
            "url": "https://esl-api.retailit.lk/api/products/by-code/{productCode}",
            "method": "GET",
            "content_type": "—",
            "auth": "Bearer token.",
            "desc":
                "The complete product record for a product code, <b>including every label bound to "
                "it</b>. A single call answers what the product is and which labels are showing it.",
            "notes": [
                "<code>storeId</code> is required because product codes are unique per store.",
                "<code>hasEsl</code> allows a caller to branch without inspecting the array.",
                "A label bound with both a template and a message appears <b>once</b>, with both sets "
                "of fields populated.",
                "<code>templateAssignmentId</code> and <code>messageAssignmentId</code> are the values "
                "sent back to <b>Part 4.4</b> to unbind a label.",
            ],
            "params": [
                p("productCode", "Path", "String", "Yes", '"PRD-1001"', "Product code to look up."),
                p("storeId", "Query", "Long", "Yes", "3", "Store the product belongs to."),
            ],
            "demo": "GET /api/products/by-code/PRD-1001?storeId=3",
            "codes": CODES_READ,
            "returns": '''{
  "success": true,
  "message": "Product retrieved successfully.",
  "responsCode": 200,
  "result": {
    "id": 6,
    "productCode": "PRD-1001",
    "barCode": "4791234500011",
    "productName": "Ceylon Black Tea 200g",
    "categoryId": 3,
    "categoryName": "Beverages",
    "subCategoryId": 4,
    "subCategoryName": "Tea & Coffee",
    "quantity": 120.0,
    "unitOfMeasure": "Pack",
    "costPrice": 780.00,
    "sellingPrice": 1050.00,
    "discountPrice": 105.00,
    "discountedPrice": 945.00,
    "isActive": true,
    "storeId": 3,
    "storeName": "Colombo City Centre",
    "hasEsl": true,
    "eslDevices": [
      {
        "deviceId": 10,
        "mac": "e0000000be65",
        "deviceName": "Aisle 1 - Shelf 2",
        "deviceType": "Minew",
        "status": "Active",
        "battery": 100,
        "isOnline": true,
        "lastSeen": "2026-08-14T09:40:00",
        "templateId": "2087930462289793024",
        "templateName": "RIT 4.2 Dynamic",
        "templateAssignmentId": 9,
        "deviceTemplateComboId": 6,
        "messageId": null,
        "messageName": null,
        "displayOrder": 1
      }
    ]
  }
}''',
        },
        {
            "name": "Query product by identifier",
            "url": "https://esl-api.retailit.lk/api/products/{id}/details",
            "method": "GET",
            "content_type": "—",
            "auth": "Bearer token.",
            "desc":
                "Identical payload to <b>Part 4.6</b>, keyed on the internal product identifier "
                "instead of the product code.",
            "params": [
                p("id", "Path", "Long", "Yes", "6", "Product identifier."),
                p("storeId", "Query", "Long", "Yes", "3", "Store the product belongs to."),
            ],
            "demo": "GET /api/products/6/details?storeId=3",
            "codes": CODES_READ,
            "returns": "As Part 4.6.",
        },
        {
            "name": "Delete product",
            "url": "https://esl-api.retailit.lk/api/products/product/{id}",
            "method": "DELETE",
            "content_type": "—",
            "auth": "Bearer token. Admin, Manager or Operator.",
            "desc":
                "Removes a product. Any label bound to it is released so it stops displaying the "
                "product.",
            "params": [
                p("id", "Path", "Long", "Yes", "6", "Product identifier."),
                p("storeId", "Query", "Long", "Yes", "3", "Store the product belongs to."),
            ],
            "demo": "DELETE /api/products/product/6?storeId=3",
            "codes": CODES_STANDARD,
            "returns": '''{
  "success": true,
  "message": "Product deleted successfully.",
  "responsCode": 200,
  "result": true
}''',
        },
    ],
})

# ------------------------------- Part 5 ------------------------------------
PARTS.append({
    "title": "Bulk API",
    "intro":
        "Load or update many products from a single JSON payload. These are the endpoints an "
        "external point-of-sale or ERP system uses. Both are <b>partial-success</b>: a row that "
        "fails is reported with a reason and the remaining rows still complete.",
    "prose": [
        ("Reading the result",
         "Both endpoints answer <code>200</code> even when some rows fail, so the HTTP status alone "
         "is not a reliable indicator. Walk <code>result.results[]</code> and match each entry back "
         "to your source row using <code>index</code>. The envelope's <code>success</code> is "
         "<code>true</code> only when every row succeeded."),
        ("Data and display are reported separately",
         "<code>eslPushed</code> indicates the product data reached the cloud. <code>eslBound</code> "
         "indicates the label is displaying it. Either can fail independently, and a row that carries "
         "no labels reports <code>eslBound: null</code>. Because the database write is committed "
         "before the cloud is contacted, a cloud outage appears as <code>success: true</code> with "
         "<code>eslPushed: false</code> rather than losing the price change."),
    ],
    "endpoints": [
        {
            "name": "Add products in batch",
            "url": "https://esl-api.retailit.lk/api/products/bulk",
            "method": "POST",
            "content_type": "application/json;charset=utf-8",
            "auth": "Bearer token. Admin, Manager or Operator.",
            "desc":
                "Creates many products from one payload, optionally with their labels, and pushes "
                "each price in the same call.",
            "notes": [
                "A category may be given as <code>categoryId</code> or as <code>categoryName</code>, "
                "so an external system need not know SmartShelf's internal identifiers. "
                "<code>categoryId</code> takes precedence when both are supplied.",
                "Set <code>\"pushToEsl\": false</code> for a data-only load such as a migration, and "
                "<code>\"bindToEsl\": false</code> to save assignments without binding the labels.",
                "A row that fails is rolled back on its own; the rest of the payload is unaffected.",
            ],
            "params": [
                p("storeId", "Body", "Long", "Yes", "3", "Store all rows belong to."),
                p("userId", "Body", "Int", "Yes", "42", "Identifier of the user making the change."),
                p("pushToEsl", "Body", "Boolean", "No", "true", "Push prices to the cloud. Defaults to true."),
                p("bindToEsl", "Body", "Boolean", "No", "true", "Bind supplied labels. Defaults to true."),
                p("products", "Body", "Array", "Yes", "—", "Rows to create. Must contain at least one entry."),
                p("&nbsp;&nbsp;productCode", "Body", "String", "Yes", '"BULK-0001"', "Unique within the store."),
                p("&nbsp;&nbsp;productName", "Body", "String", "Yes", '"Bulk Item One"', "Display name."),
                p("&nbsp;&nbsp;categoryId", "Body", "Long", "No*", "3", "* Either this or <code>categoryName</code> is required."),
                p("&nbsp;&nbsp;categoryName", "Body", "String", "No*", '"Beverages"', "Matched exactly against active categories in the store."),
                p("&nbsp;&nbsp;sellingPrice", "Body", "Decimal", "Yes", "450.00", "Shelf price."),
                p("&nbsp;&nbsp;eslAssignments", "Body", "Array", "No", "—", "Labels to bind, as in Part 4.2."),
            ],
            "demo": '''{
  "storeId": 3,
  "userId": 42,
  "pushToEsl": true,
  "bindToEsl": true,
  "products": [
    {
      "productCode": "BULK-0001",
      "productName": "Bulk Item One",
      "barCode": "4791234510001",
      "categoryName": "Beverages",
      "quantity": 100,
      "unitOfMeasure": "Pack",
      "costPrice": 300.00,
      "sellingPrice": 450.00,
      "discountPrice": 25.00,
      "isActive": true
    },
    {
      "productCode": "BULK-0002",
      "productName": "Bulk Item Two",
      "categoryId": 3,
      "sellingPrice": 890.00,
      "isActive": true,
      "eslAssignments": [
        { "deviceId": 10, "templateId": "2087930462289793024", "displayOrder": 1 }
      ]
    }
  ]
}''',
            "codes": [
                ("200", "The batch was processed. Individual rows may still have failed."),
                ("400", "The payload was empty, or <code>storeId</code> was missing or unknown."),
                ("403", "The user's role does not permit creating products."),
                ("500", "Unexpected server error before the batch began."),
            ],
            "returns": '''{
  "success": false,
  "message": "Bulk create finished: 2 created, 1 failed.",
  "responsCode": 200,
  "result": {
    "storeId": 3,
    "totalRows": 3,
    "succeeded": 2,
    "failed": 1,
    "eslPushSucceeded": 2,
    "eslPushFailed": 0,
    "eslBoundSucceeded": 1,
    "eslBoundFailed": 0,
    "eslSummary": "ESL push: 2 succeeded, 0 failed. Label bind: 1 succeeded, 0 failed.",
    "results": [
      { "index": 0, "productCode": "BULK-0001", "productId": 18, "success": true,
        "message": "Created.", "eslPushed": true, "eslMessage": "Pushed to ESL.",
        "eslBound": null, "eslBindMessage": null },
      { "index": 1, "productCode": "BULK-0002", "productId": 19, "success": true,
        "message": "Created.", "eslPushed": true, "eslMessage": "Pushed to ESL.",
        "eslBound": true, "eslBindMessage": "1 label(s) bound." },
      { "index": 2, "productCode": "BULK-0003", "productId": null, "success": false,
        "message": "Category not found (CategoryId=, CategoryName='Unknown').",
        "eslPushed": null, "eslMessage": null, "eslBound": null, "eslBindMessage": null }
    ]
  }
}''',
        },
        {
            "name": "Modify products and prices in batch",
            "url": "https://esl-api.retailit.lk/api/products/bulk",
            "method": "PUT",
            "content_type": "application/json;charset=utf-8",
            "auth": "Bearer token. Admin, Manager or Operator.",
            "desc":
                "Updates many products from one payload and pushes every new price to its label in "
                "the same request. This is the endpoint a price feed should use.",
            "notes": [
                "Rows are matched on <code>productCode</code> within <code>storeId</code>. Send "
                "<code>id</code> instead to match on the internal identifier.",
                "<b>Only the fields supplied are changed.</b> Every field is optional, so the "
                "price-only payload below will not blank descriptions, barcodes or categories. This "
                "is the difference from <b>Part 4.3</b>, which replaces the whole record.",
                "Labels already bound continue to display the product; there is no need to re-bind "
                "after a price change.",
            ],
            "params": [
                p("storeId", "Body", "Long", "Yes", "3", "Store all rows belong to."),
                p("userId", "Body", "Int", "Yes", "42", "Identifier of the user making the change."),
                p("pushToEsl", "Body", "Boolean", "No", "true", "Push prices to the cloud. Defaults to true."),
                p("products", "Body", "Array", "Yes", "—", "Rows to update."),
                p("&nbsp;&nbsp;productCode", "Body", "String", "Yes*", '"PRD-1001"', "* Either this or <code>id</code> is required."),
                p("&nbsp;&nbsp;id", "Body", "Long", "No*", "6", "Match on the internal identifier instead."),
                p("&nbsp;&nbsp;sellingPrice", "Body", "Decimal", "No", "1199.50", "Any field may be omitted to leave it unchanged."),
                p("&nbsp;&nbsp;discountPrice", "Body", "Decimal", "No", "99.50", "Discount amount."),
            ],
            "demo": '''{
  "storeId": 3,
  "userId": 42,
  "pushToEsl": true,
  "products": [
    { "productCode": "PRD-1001", "sellingPrice": 1199.50, "discountPrice": 99.50 },
    { "productCode": "PRD-1002", "sellingPrice": 1750.00 }
  ]
}''',
            "codes": [
                ("200", "The batch was processed. Individual rows may still have failed."),
                ("400", "The payload was empty, or <code>storeId</code> was missing or unknown."),
                ("403", "The user's role does not permit updating products."),
                ("500", "Unexpected server error before the batch began."),
            ],
            "returns": '''{
  "success": false,
  "message": "Bulk update finished: 2 updated, 1 failed.",
  "responsCode": 200,
  "result": {
    "storeId": 3,
    "totalRows": 3,
    "succeeded": 2,
    "failed": 1,
    "eslPushSucceeded": 2,
    "eslPushFailed": 0,
    "eslSummary": "ESL push: 2 succeeded, 0 failed.",
    "results": [
      { "index": 0, "productCode": "PRD-1001", "productId": 6, "success": true,
        "message": "Updated.", "eslPushed": true, "eslMessage": "Pushed to ESL." },
      { "index": 1, "productCode": "PRD-1002", "productId": 7, "success": true,
        "message": "Updated.", "eslPushed": true, "eslMessage": "Pushed to ESL." },
      { "index": 2, "productCode": "NOPE-999", "productId": null, "success": false,
        "message": "Product code 'NOPE-999' not found in store 3.",
        "eslPushed": null, "eslMessage": null }
    ]
  }
}''',
        },
    ],
})

# ------------------------------- Part 6 ------------------------------------
PARTS.append({
    "title": "Label API",
    "intro":
        "Inspect an individual electronic shelf label and, where necessary, force it to redraw. "
        "Labels are normally bound as part of creating or updating a product; the endpoints here "
        "exist for diagnosis and recovery. The last five manage the pairings of label and template "
        "that a redraw refers to.",
    "endpoints": [
        {
            "name": "Query label by MAC address",
            "url": "https://esl-api.retailit.lk/api/device/device/by-mac/{mac}",
            "method": "GET",
            "content_type": "—",
            "auth": "Bearer token.",
            "desc":
                "The complete record for a label, <b>including what it is currently displaying</b>. "
                "This is the first call to make when a label is showing something unexpected.",
            "notes": [
                "The MAC address may be given bare (<code>e0000000be65</code>) or separated "
                "(<code>e0:00:00:00:be:65</code> or <code>e0-00-00-00-be-65</code>). Both match the "
                "same label, and case is not significant.",
                "<code>storeId</code> is optional and needed only to distinguish a MAC address "
                "registered in more than one store.",
            ],
            "params": [
                p("mac", "Path", "String", "Yes", '"e0000000be65"', "Label MAC address, bare or separated."),
                p("storeId", "Query", "Long", "No", "3", "Disambiguates a MAC present in several stores."),
            ],
            "demo": "GET /api/device/device/by-mac/e0000000be65?storeId=3",
            "codes": CODES_READ,
            "returns": '''{
  "success": true,
  "message": "Device retrieved successfully.",
  "responsCode": 200,
  "result": {
    "id": 10,
    "mac": "e0000000be65",
    "deviceName": "Aisle 1 - Shelf 2",
    "deviceType": "Minew",
    "minewDeviceId": "2087929882007834624",
    "status": "Active",
    "battery": 100,
    "isOnline": true,
    "isActive": true,
    "lastSeen": "2026-08-14T09:40:00",
    "lastSyncTime": "2026-08-14T09:38:00",
    "firmware": "3.5.0",
    "screenInch": 4.20,
    "screenWidth": 400,
    "screenHeight": 300,
    "screenColor": "bwry",
    "storeId": 3,
    "storeName": "Colombo City Centre",
    "minewStoreId": "2087891877587062784",
    "isBound": true,
    "bindings": [
      {
        "assignmentId": 9,
        "assignmentType": "TEMPLATE",
        "locationType": "Product",
        "locationId": 6,
        "productId": 6,
        "productCode": "PRD-1001",
        "productName": "Ceylon Black Tea 200g",
        "sellingPrice": 1050.00,
        "discountPrice": 105.00,
        "templateId": "2087930462289793024",
        "templateName": "RIT 4.2 Dynamic",
        "messageId": null,
        "messageName": null,
        "displayOrder": 1
      }
    ]
  }
}''',
        },
        {
            "name": "Query label by identifier",
            "url": "https://esl-api.retailit.lk/api/device/device/{id}",
            "method": "GET",
            "content_type": "—",
            "auth": "Bearer token.",
            "desc":
                "Label record by internal identifier. Returns the device fields only; use "
                "<b>Part 6.1</b> for the version that includes what the label is displaying.",
            "params": [
                p("id", "Path", "Long", "Yes", "10", "Label identifier."),
            ],
            "demo": "GET /api/device/device/10",
            "codes": CODES_READ,
            "returns": '''{
  "success": true,
  "message": "Device retrieved successfully.",
  "responsCode": 200,
  "result": {
    "id": 10,
    "mac": "e0000000be65",
    "deviceName": "Aisle 1 - Shelf 2",
    "status": "Active",
    "battery": 100,
    "isOnline": true,
    "storeId": 3,
    "storeName": "Colombo City Centre"
  }
}''',
        },
        {
            "name": "Redraw label",
            "url": "https://esl-api.retailit.lk/api/device/bind",
            "method": "POST",
            "content_type": "application/json;charset=utf-8",
            "auth": "Bearer token. Admin or Manager only.",
            "desc":
                "Forces a label to redraw a product it is already assigned to.",
            "notes": [
                "This is not normally required. Creating or updating a product with "
                "<code>eslAssignments</code> binds the label automatically.",
                "Use it to recover a label that was asleep or out of range when it was bound, or "
                "after a template was changed in the Minew console.",
                "<code>comboId</code> is the <code>deviceTemplateComboId</code> returned by "
                "<b>Part 4.6</b>. When the product is not known, find the pairing with "
                "<b>Part 6.7</b> filtered by label, or create-or-reuse it with <b>Part 6.6</b>, "
                "which returns the same identifier.",
            ],
            "params": [
                p("comboId", "Body", "Long", "Yes", "6", "Label and template pairing to redraw."),
                p("productId", "Body", "Long", "Yes*", "6", "* Supply this, or an explicit <code>goodsMap</code>."),
                p("goodsMap", "Body", "Object", "No*", "—", "Explicit field values, when not deriving them from a product."),
            ],
            "demo": '''{
  "comboId": 6,
  "productId": 6
}''',
            "codes": [
                ("200", "The label was instructed to redraw."),
                ("400", "The store is not linked to a Minew store, or the cloud rejected the request."),
                ("403", "The user's role does not permit binding."),
                ("404", "Unknown pairing or product."),
                ("500", "Unexpected server error."),
            ],
            "returns": '''{
  "message": "Data bound successfully",
  "response": {
    "code": 200,
    "msg": "success",
    "data": null
  }
}''',
        },
        {
            "name": "Register a label",
            "url": "https://esl-api.retailit.lk/api/device/device",
            "method": "POST",
            "content_type": "application/json;charset=utf-8",
            "auth": "Bearer token. Admin, Manager or Operator.",
            "desc":
                "Records a label in SmartShelf. This creates the local record only — see "
                "<b>Part 6.5</b> to register the same label with the Minew cloud, which is what "
                "makes it addressable.",
            "notes": [
                "<code>macAddress</code> is accepted either bare (<code>e0000000be65</code>) or "
                "separated (<code>E0:00:00:00:BE:65</code>). It is stored bare and matched that "
                "way, so the two forms are the same label.",
                "Screen size is left empty here: the panel dimensions are only knowable from the "
                "cloud. They are filled in when the label is next read back from Minew, and the "
                "label can be bound before that happens.",
                "A MAC already registered and active in the same store is rejected with "
                "<code>409</code>. A label that was deleted earlier is revived by this call "
                "rather than duplicated, keeping its identifier and history.",
            ],
            "params": [
                p("macAddress", "Body", "String", "Yes", '"e0000000be65"', "12 hex characters, with or without separators."),
                p("name", "Body", "String", "Yes", '"Aisle 1 - Shelf 2"', "Display name. Maximum 100 characters."),
                p("deviceType", "Body", "String", "Yes", '"Minew"', "<code>Minew</code> or <code>Standard</code>."),
                p("storeId", "Body", "Long", "Yes", "3", "Store the label belongs to. The SmartShelf store id."),
                p("screenId", "Body", "Long", "No", "null", "Panel size, when already known. Normally left null."),
                p("ipAddress", "Body", "String", "No", '""', "Standard devices only. Empty or an IPv4 address."),
                p("battery", "Body", "Int", "No", "100", "Battery percentage. Defaults to 100."),
                p("statusId", "Body", "Int", "No", "1", "1 Active, 2 Inactive, 3 Offline, 4 Maintenance."),
                p("isActive", "Body", "Boolean", "No", "true", "Defaults to true."),
                p("createdUser", "Body", "Int", "No", "42", "Identifier of the user making the change."),
            ],
            "demo": '''{
  "macAddress": "e0000000be65",
  "name": "Aisle 1 - Shelf 2",
  "deviceType": "Minew",
  "storeId": 3,
  "battery": 100,
  "statusId": 1,
  "isActive": true,
  "createdUser": 42
}''',
            "codes": [
                ["200", "Label registered."],
                ["400", "A required field is missing, or the MAC or IP address is malformed."],
                ["409", "That MAC is already registered and active in this store."],
                ["500", "Unexpected server error."],
            ],
            "returns": '''{
  "success": true,
  "message": "Device created successfully",
  "responsCode": 200,
  "result": {
    "id": 13,
    "mac": "e0000000be65",
    "deviceName": "Aisle 1 - Shelf 2",
    "deviceType": "Minew",
    "screenId": null,
    "screenInch": null,
    "screenWidth": null,
    "screenHeight": null,
    "status": "Active",
    "battery": 100,
    "storeId": 3,
    "storeName": "Colombo City Centre",
    "isOnline": false
  }
}''',
        },
        {
            "name": "Add labels to the cloud",
            "url": "https://esl-api.retailit.lk/api/device/devices/batch-add-minew",
            "method": "POST",
            "content_type": "application/json;charset=utf-8",
            "auth": "Bearer token. Admin or Manager.",
            "desc":
                "Registers labels with the Minew cloud so they can be written to. Send the labels "
                "recorded by <b>Part 6.4</b>; a label that is not in the cloud cannot be bound.",
            "notes": [
                "<b><code>storeId</code> here is the Minew cloud store identifier</b>, not the "
                "SmartShelf store id used everywhere else in this document. Read it from the "
                "store's <code>minewStoreId</code>. Sending the local id fails with "
                "<i>store does not exist</i>.",
                "The call reports on each MAC separately in <code>results</code>. A label already "
                "registered in the cloud is counted in <code>failedCount</code> — it is a no-op "
                "rather than an error, so read the per-MAC entries rather than the counts alone.",
                "<b>The per-MAC strings come straight from the Minew cloud and are not "
                "translated.</b> They are usually Chinese — an already-registered label answers "
                "<code>标签已被添加</code> (<i>label already added</i>). Treat them as opaque "
                "vendor text for display or logging; do not branch on their wording, since Minew "
                "can change it. Use <code>addedCount</code> and <code>failedCount</code>, or "
                "re-read the label with <b>Part 6.1</b>, to decide what actually happened.",
                "Successfully added labels are then woken so the cloud learns their panel size. "
                "Their screen dimensions appear in SmartShelf on the next read from the cloud.",
            ],
            "params": [
                p("storeId", "Body", "String", "Yes", '"1967816075357720576"', "Minew cloud store identifier. Quoted — it exceeds 32 bits."),
                p("macAddresses", "Body", "Array", "Yes", '["e0000000be65"]', "Labels to register. Bare or separated MACs."),
                p("type", "Body", "Int", "No", "1", "1 electronic shelf label, 5 warning light. Defaults to 1."),
                p("userId", "Body", "Int", "No", "42", "Identifier of the user making the change."),
            ],
            "demo": '''{
  "storeId": "1967816075357720576",
  "macAddresses": ["e0000000be65", "c300002e8c2a"],
  "type": 1,
  "userId": 42
}''',
            "codes": [
                ["200", "The cloud accepted the request. Read <code>results</code> per label."],
                ["400", "No MAC addresses supplied, or the store is unknown to the cloud."],
                ["500", "Unexpected server error."],
            ],
            "returns": '''{
  "success": true,
  "message": "Devices added successfully. 1 devices added.",
  "responsCode": 200,
  "result": {
    "addedCount": 1,
    "failedCount": 1,
    "results": {
      "e0000000be65": "success",
      "c300002e8c2a": "标签已被添加"
    },
    "wakeUpResult": {
      "success": true,
      "wokeCount": 1
    },
    "processedAt": "2026-02-11T09:14:22Z"
  }
}''',
        },
        {
            "name": "Pair a label with a template",
            "url": "https://esl-api.retailit.lk/api/device/combos",
            "method": "POST",
            "content_type": "application/json;charset=utf-8",
            "auth": "Bearer token. Admin or Manager only.",
            "desc":
                "Pairs a label with a template. The identifier returned is the "
                "<code>comboId</code> that <b>Part 6.3</b> redraws, and the "
                "<code>deviceTemplateComboId</code> reported by <b>Part 4.6</b>.",
            "notes": [
                "This is not normally required. Creating or updating a product with "
                "<code>eslAssignments</code> makes the pairing automatically.",
                "The call is safe to repeat. A pairing that already exists is returned as it "
                "stands with <code>alreadyExists: true</code>, and nothing is created — so a "
                "caller that does not know whether the pair exists can send this and read the "
                "identifier from the reply either way.",
                "The reply is a plain object, <b>not</b> the standard envelope described in "
                "Appendix A.",
                "A template belongs to one store and one screen size. This call accepts a "
                "mismatched pair; the cloud rejects it later when the label is bound. Confirm the "
                "template with <b>Part 3.5</b> first.",
            ],
            "params": [
                p("deviceId", "Body", "Int", "Yes", "13", "Label to pair, by SmartShelf identifier."),
                p("templateId", "Body", "String", "Yes", '"2087661050496290816"', "Template to pair. Keep quoted."),
                p("isDefault", "Body", "Boolean", "No", "false", "Marks the pairing as the label's standing template."),
            ],
            "demo": '''{
  "deviceId": 13,
  "templateId": "2087661050496290816",
  "isDefault": false
}''',
            "codes": [
                ("200", "The pairing was created, or an existing one was returned."),
                ("400", "<code>deviceId</code> or <code>templateId</code> was not supplied."),
                ("403", "The user's role does not permit this operation."),
                ("404", "No such label, or no such template."),
                ("500", "Unexpected server error."),
            ],
            "returns": '''{
  "id": 6,
  "deviceId": 13,
  "templateId": "2087661050496290816",
  "deviceName": "Minew Device 00be65",
  "templateName": "4.2_BWRY-2",
  "screenWidth": 400,
  "screenHeight": 300,
  "isDefault": false,
  "priority": 0,
  "createdDate": "2026-08-14T05:05:43.9913458",
  "alreadyExists": true
}''',
        },
        {
            "name": "Query pairing list",
            "url": "https://esl-api.retailit.lk/api/device/combos/paged",
            "method": "GET",
            "content_type": "—",
            "auth": "Bearer token.",
            "desc":
                "Every label and template pairing, paged and filterable. This is how to find the "
                "<code>comboId</code> of a pairing that already exists — filter by label, by "
                "template, or by both.",
            "notes": [
                "Filtering by <code>deviceId</code> and <code>templateId</code> together returns "
                "the single pairing for that pair, which is the quickest route to its "
                "<code>comboId</code>.",
                "<code>isActive</code> defaults to <code>true</code>, so deleted pairings are "
                "excluded unless it is set to <code>false</code> explicitly.",
            ],
            "params": [
                p("pageNumber", "Query", "Int", "No", "1", "Page to return. Defaults to 1."),
                p("pageSize", "Query", "Int", "No", "20", "Rows per page."),
                p("deviceId", "Query", "Long", "No", "13", "Restricts the list to one label."),
                p("templateId", "Query", "String", "No", '"2087661050496290816"', "Restricts the list to one template."),
                p("isDefault", "Query", "Boolean", "No", "false", "Restricts to standing pairings."),
                p("isActive", "Query", "Boolean", "No", "true", "Defaults to true."),
                p("searchTerm", "Query", "String", "No", '"4.2"', "Matches the label or template name."),
                p("sortBy", "Query", "String", "No", '"createdDate"', "Field to order by."),
                p("sortDescending", "Query", "Boolean", "No", "true", "Reverses the order."),
            ],
            "demo": "GET /api/device/combos/paged?pageNumber=1&pageSize=20&deviceId=13",
            "codes": [
                ("200", "Success."),
                ("401", "Missing, expired or invalid bearer token."),
                ("500", "Unexpected server error."),
            ],
            "returns": '''{
  "success": true,
  "message": "Device template combos retrieved successfully",
  "result": {
    "items": [
      {
        "id": 6,
        "type": "TEMPLATE",
        "deviceId": 13,
        "deviceName": "Minew Device 00be65",
        "deviceMac": "e0000000be65",
        "templateId": "2087661050496290816",
        "templateName": "4.2_BWRY-2",
        "screenWidth": 400,
        "screenHeight": 300,
        "screenInch": 4.20,
        "battery": 100,
        "isDefault": false,
        "priority": 0,
        "isActive": true,
        "createdDate": "2026-08-14T05:05:43.9913458"
      }
    ],
    "totalCount": 2,
    "pageNumber": 1,
    "pageSize": 20
  }
}''',
        },
        {
            "name": "Query a pairing",
            "url": "https://esl-api.retailit.lk/api/device/combos/{id}",
            "method": "GET",
            "content_type": "—",
            "auth": "Bearer token.",
            "desc":
                "One pairing, with the label's MAC address and the template's screen size. Use it "
                "to confirm a <code>comboId</code> before redrawing with <b>Part 6.3</b>.",
            "notes": [
                "An identifier that does not exist answers <code>404</code>.",
            ],
            "params": [
                p("id", "Path", "Long", "Yes", "6", "Pairing identifier."),
            ],
            "demo": "GET /api/device/combos/6",
            "codes": [
                ("200", "The pairing was returned."),
                ("401", "Missing, expired or invalid bearer token."),
                ("404", "No pairing has that identifier."),
                ("500", "Unexpected server error."),
            ],
            "returns": '''{
  "success": true,
  "message": "DeviceTemplateCombo retrieved successfully.",
  "responsCode": 200,
  "result": {
    "id": 6,
    "type": "TEMPLATE",
    "deviceId": 13,
    "deviceName": "Minew Device 00be65",
    "deviceMac": "e0000000be65",
    "templateId": "2087661050496290816",
    "templateName": "4.2_BWRY-2",
    "screenWidth": 400,
    "screenHeight": 300,
    "screenInch": 4.20,
    "battery": 100,
    "isDefault": false,
    "priority": 0,
    "isActive": true,
    "createdDate": "2026-08-14T05:05:43.9913458"
  }
}''',
        },
        {
            "name": "Modify a pairing",
            "url": "https://esl-api.retailit.lk/api/device/combos/{id}",
            "method": "PUT",
            "content_type": "application/json;charset=utf-8",
            "auth": "Bearer token. Admin or Manager only.",
            "desc":
                "Repoints a pairing at a different label or template, or changes whether it is the "
                "label's standing pairing.",
            "notes": [
                "All four body fields are written on every call. A field that is omitted is "
                "written as its default — send the pairing's current values for anything that "
                "should not change.",
                "Repointing a pairing onto a label and template that another active pairing "
                "already covers answers <code>409</code> and changes nothing.",
                "The reply carries the stored record rather than the fuller shape returned by "
                "<b>Part 6.8</b>; the label and template names are not included.",
                "This does not redraw the label. Follow with <b>Part 6.3</b> to push the change.",
            ],
            "params": [
                p("id", "Path", "Long", "Yes", "6", "Pairing to modify."),
                p("deviceId", "Body", "Int", "Yes", "13", "Label the pairing points at."),
                p("templateId", "Body", "String", "Yes", '"2087661050496290816"', "Template the pairing points at."),
                p("isDefault", "Body", "Boolean", "No", "false", "Marks it as the label's standing template."),
                p("isActive", "Body", "Boolean", "No", "true", "Set false to retire the pairing."),
            ],
            "demo": '''{
  "deviceId": 13,
  "templateId": "2087661050496290816",
  "isDefault": true,
  "isActive": true
}''',
            "codes": [
                ("200", "The pairing was updated."),
                ("400", "<code>deviceId</code> or <code>templateId</code> was not supplied."),
                ("403", "The user's role does not permit this operation."),
                ("404", "No pairing has that identifier."),
                ("409", "Another active pairing already covers that label and template."),
                ("500", "Unexpected server error."),
            ],
            "returns": '''{
  "success": true,
  "message": "DeviceTemplateCombo updated successfully",
  "responsCode": 200,
  "result": {
    "id": 6,
    "deviceId": 13,
    "templateId": "2087661050496290816",
    "isDefault": false,
    "isActive": true,
    "priority": 0,
    "createdDate": "2026-08-14T05:05:43.9913458",
    "updatedDate": "2026-08-14T06:49:15.5271232Z",
    "updatedUser": 0
  }
}''',
        },
        {
            "name": "Delete a pairing",
            "url": "https://esl-api.retailit.lk/api/device/combos/{id}/user/{userId}",
            "method": "DELETE",
            "content_type": "—",
            "auth": "Bearer token. Admin or Manager only.",
            "desc":
                "Retires a pairing. The record is kept and marked inactive rather than removed, so "
                "the history of what a label displayed stays intact.",
            "notes": [
                "A pairing still used by an active product binding cannot be retired. That refusal "
                "answers <code>409</code> and names the reason in <code>message</code>. Unbind the "
                "product first with <b>Part 4.4</b>, then retire the pairing.",
                "Retiring a pairing does not clear the label. It goes on showing whatever was last "
                "written to it.",
            ],
            "params": [
                p("id", "Path", "Long", "Yes", "6", "Pairing to retire."),
                p("userId", "Path", "Int", "Yes", "42", "Identifier of the user making the change."),
            ],
            "demo": "DELETE /api/device/combos/6/user/42",
            "codes": [
                ("200", "The pairing was retired."),
                ("403", "The user's role does not permit this operation."),
                ("404", "No pairing has that identifier."),
                ("409", "The pairing is still used by an active product binding."),
                ("500", "Unexpected server error."),
            ],
            "returns": '''{
  "success": true,
  "message": "Combo deleted successfully",
  "responsCode": 200,
  "result": true
}''',
        },
    ],
})

# ------------------------------- Part 7 ------------------------------------
PARTS.append({
    "title": "Scheduling API",
    "intro":
        "Schedule a product onto a label for a defined period — a promotion window, a happy-hour "
        "price, a seasonal display. A background service arms each schedule and executes it at the "
        "appointed time.",
    "prose": [
        ("The price is read when the schedule runs",
         "Not when it is created. A promotion scheduled today picks up whatever the selling price is "
         "at the moment it executes, so a recurring schedule continues to show the current price "
         "without being amended."),
        ("Times are UTC",
         "The scheduler compares against UTC. Sri Lanka Standard Time is UTC+5:30, so a promotion "
         "intended to begin at 15:00 local time must be sent as <code>09:30</code>. This is the most "
         "common cause of a schedule that appears never to run."),
        ("Execution timing",
         "The service checks for due schedules every 30 seconds and activates at most 20 in each "
         "cycle. This is appropriate for promotions and price changes; it is not intended for "
         "second-accurate work."),
    ],
    "endpoints": [
        {
            "name": "Add schedule",
            "url": "https://esl-api.retailit.lk/api/queue/direct",
            "method": "POST",
            "content_type": "application/json;charset=utf-8",
            "auth": "Bearer token. Admin, Manager or Operator.",
            "desc":
                "Schedules a product onto a label between a start and end time.",
            "notes": [
                "<code>startDate</code> and <code>endDate</code> are <b>UTC</b>.",
                "Always supply <code>endDate</code>. The overlap check cannot compare against an "
                "open-ended schedule, so omitting it allows conflicting schedules to be created.",
                "<code>templateId</code> is required for Minew labels even when the purpose of the "
                "schedule is to show a message.",
                "<code>storeId</code> is not sent; it is taken from the label.",
            ],
            "params": [
                p("deviceId", "Body", "Long", "Yes", "10", "Label to schedule, from Part 3.3."),
                p("templateId", "Body", "String", "Yes", '"2087930462289793024"', "Template to render with. Keep quoted."),
                p("messageId", "Body", "Long", "No", "null", "Optional message rendered alongside the template."),
                p("locationType", "Body", "String", "Yes", '"PRODUCT"', "<code>PRODUCT</code> or <code>SHELF</code>."),
                p("locationId", "Body", "Long", "Yes", "6", "Product identifier when the type is <code>PRODUCT</code>."),
                p("startDate", "Body", "DateTime", "Yes", '"2026-08-15T09:30:00"', "UTC. Format <code>yyyy-MM-ddTHH:mm:ss</code>."),
                p("endDate", "Body", "DateTime", "No", '"2026-08-15T14:30:00"', "UTC. Strongly recommended — see note above."),
                p("priorityId", "Body", "Int", "No", "2", "See Part 7.11. Defaults to 4 (Scheduled)."),
                p("isRecurring", "Body", "Boolean", "No", "false", "Repeat according to <code>recurrencePattern</code>."),
                p("recurrencePattern", "Body", "String", "No", '"DAILY"', "<code>DAILY</code>, <code>WEEKLY</code>, <code>MONTHLY</code> or <code>HOURLY</code>."),
                p("userId", "Body", "Int", "Yes", "42", "Identifier of the user making the change."),
                p("queueType", "Body", "String", "No", '"TEMPLATE_QUEUE"', "Defaults by label type."),
            ],
            "demo": '''{
  "deviceId": 10,
  "templateId": "2087930462289793024",
  "messageId": null,
  "locationType": "PRODUCT",
  "locationId": 6,
  "startDate": "2026-08-15T09:30:00",
  "endDate": "2026-08-15T14:30:00",
  "priorityId": 2,
  "isRecurring": false,
  "recurrencePattern": null,
  "userId": 42,
  "queueType": "TEMPLATE_QUEUE"
}''',
            "codes": [
                ("201", "Scheduled."),
                ("400", "Invalid request. A Minew label was scheduled without a template, or the location does not exist."),
                ("409", "An unfinished schedule already covers this label and template over an overlapping period."),
                ("500", "Unexpected server error."),
            ],
            "returns": '''{
  "success": true,
  "message": "Queue created successfully",
  "responsCode": 201,
  "result": {
    "id": 13,
    "queueType": "TEMPLATE_QUEUE",
    "deviceName": "Aisle 1 - Shelf 2",
    "deviceType": "Minew",
    "locationType": "PRODUCT",
    "locationName": "Ceylon Black Tea 200g",
    "productId": 6,
    "productName": "Ceylon Black Tea 200g",
    "startDate": "2026-08-15T09:30:00Z",
    "endDate": "2026-08-15T14:30:00Z",
    "isActive": true,
    "isRecurring": false,
    "displayOrder": 1,
    "storeId": 3
  }
}''',
        },
        {
            "name": "Add schedule from an existing binding",
            "url": "https://esl-api.retailit.lk/api/queue/from-assignment",
            "method": "POST",
            "content_type": "application/json;charset=utf-8",
            "auth": "Bearer token. Admin, Manager or Operator.",
            "desc":
                "Schedules a label that is already bound, so the label, template and product need "
                "not be repeated.",
            "notes": [
                "<code>assignmentId</code> is the <code>templateAssignmentId</code> returned by "
                "<b>Part 4.6</b>.",
            ],
            "params": [
                p("assignmentId", "Body", "Long", "Yes", "9", "Existing binding to schedule."),
                p("startDate", "Body", "DateTime", "Yes", '"2026-08-15T09:30:00"', "UTC."),
                p("endDate", "Body", "DateTime", "No", '"2026-08-15T14:30:00"', "UTC."),
                p("priorityId", "Body", "Int", "No", "2", "See Part 7.11."),
                p("displayOrder", "Body", "Int", "No", "1", "Order among several labels."),
                p("isRecurring", "Body", "Boolean", "No", "false", "Repeat the schedule."),
                p("recurrencePattern", "Body", "String", "No", "null", "Recurrence interval."),
                p("userId", "Body", "Int", "Yes", "42", "Identifier of the user making the change."),
            ],
            "demo": '''{
  "assignmentId": 9,
  "startDate": "2026-08-15T09:30:00",
  "endDate": "2026-08-15T14:30:00",
  "priorityId": 2,
  "displayOrder": 1,
  "isRecurring": false,
  "recurrencePattern": null,
  "userId": 42
}''',
            "codes": [
                ("201", "Scheduled."),
                ("400", "Invalid request."),
                ("404", "Unknown binding."),
                ("409", "An overlapping schedule already exists."),
                ("500", "Unexpected server error."),
            ],
            "returns": "As Part 7.1.",
        },
        {
            "name": "Query schedule list",
            "url": "https://esl-api.retailit.lk/api/queue",
            "method": "GET",
            "content_type": "—",
            "auth": "Bearer token.",
            "desc": "Paged list of schedules, with filtering.",
            "params": [
                p("storeId", "Query", "Long", "No", "3", "Restricts to one store."),
                p("deviceId", "Query", "Long", "No", "10", "Restricts to one label."),
                p("status", "Query", "String", "No", '"Pending"', "<code>Pending</code>, <code>Processing</code>, <code>Completed</code> or <code>Failed</code>."),
                p("locationType", "Query", "String", "No", '"PRODUCT"', "Restricts by location type."),
                p("isActive", "Query", "Boolean", "No", "true", "Restricts to live or retired schedules."),
                p("pageNumber", "Query", "Int", "No", "1", "Page number."),
                p("pageSize", "Query", "Int", "No", "20", "Rows per page."),
            ],
            "demo": "GET /api/queue?storeId=3&status=Pending&pageNumber=1&pageSize=20",
            "codes": CODES_READ,
            "returns": '''{
  "success": true,
  "message": "Queues retrieved successfully",
  "responsCode": 200,
  "result": {
    "items": [
      {
        "id": 13,
        "queueType": "TEMPLATE_QUEUE",
        "deviceName": "Aisle 1 - Shelf 2",
        "locationType": "PRODUCT",
        "locationName": "Ceylon Black Tea 200g",
        "startDate": "2026-08-15T09:30:00Z",
        "endDate": "2026-08-15T14:30:00Z",
        "status": "Pending",
        "priority": "Price Change",
        "isActive": true
      }
    ],
    "totalCount": 1,
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 1
  }
}''',
        },
        {
            "name": "Query schedule and execution history",
            "url": "https://esl-api.retailit.lk/api/queue/{id}",
            "method": "GET",
            "content_type": "—",
            "auth": "Bearer token.",
            "desc":
                "One schedule together with a record of every attempt to execute it.",
            "notes": [
                "When a label did not change, this is where the reason is. <code>errorMessage</code> "
                "carries the cloud's own rejection text and <code>bindingData</code> holds its raw "
                "response.",
                "A rejected instruction is recorded as <b>Failed</b> rather than silently marked "
                "complete, so a status of Failed always reflects a genuine attempt.",
            ],
            "params": [
                p("id", "Path", "Long", "Yes", "13", "Schedule identifier."),
            ],
            "demo": "GET /api/queue/13",
            "codes": CODES_READ,
            "returns": '''{
  "success": true,
  "message": "Queue retrieved successfully",
  "responsCode": 200,
  "result": {
    "id": 13,
    "deviceName": "Aisle 1 - Shelf 2",
    "productName": "Ceylon Black Tea 200g",
    "startDate": "2026-08-15T09:30:00Z",
    "endDate": "2026-08-15T14:30:00Z",
    "status": "Completed",
    "priority": "Price Change",
    "lastAttempt": "2026-08-15T09:30:12Z",
    "retryCount": 0,
    "errorMessage": null,
    "bindingData": "{ \\"code\\": 200, \\"msg\\": \\"success\\", \\"data\\": null }"
  }
}''',
        },
        {
            "name": "Query upcoming schedules",
            "url": "https://esl-api.retailit.lk/api/queue/upcoming",
            "method": "GET",
            "content_type": "—",
            "auth": "Bearer token.",
            "desc":
                "Schedules armed to run within the next given number of hours. The quickest check "
                "that a schedule was accepted and will execute.",
            "params": [
                p("hours", "Query", "Int", "No", "24", "Look-ahead window. Defaults to 24."),
            ],
            "demo": "GET /api/queue/upcoming?hours=24",
            "codes": CODES_READ,
            "returns": '''{
  "success": true,
  "message": "Upcoming queues retrieved successfully",
  "responsCode": 200,
  "result": [
    {
      "id": 13,
      "locationType": "PRODUCT",
      "locationName": "Ceylon Black Tea 200g",
      "productId": 6,
      "productName": "Ceylon Black Tea 200g",
      "deviceName": "Aisle 1 - Shelf 2",
      "startDate": "2026-08-15T09:30:00Z",
      "status": "Pending"
    }
  ]
}''',
        },
        {
            "name": "Query active schedules",
            "url": "https://esl-api.retailit.lk/api/queue/active",
            "method": "GET",
            "content_type": "—",
            "auth": "Bearer token.",
            "desc": "Schedules currently within their display period.",
            "params": [],
            "demo": "GET /api/queue/active",
            "codes": CODES_READ,
            "returns": "Same shape as Part 7.5.",
        },
        {
            "name": "Query schedules for a label",
            "url": "https://esl-api.retailit.lk/api/queue/device/{deviceId}",
            "method": "GET",
            "content_type": "—",
            "auth": "Bearer token.",
            "desc":
                "Every schedule targeting one label. Useful when a label is showing something "
                "unexpected and the cause may be a schedule rather than a direct binding.",
            "params": [
                p("deviceId", "Path", "Long", "Yes", "10", "Label identifier."),
            ],
            "demo": "GET /api/queue/device/10",
            "codes": CODES_READ,
            "returns": "Same shape as Part 7.5.",
        },
        {
            "name": "Modify schedule",
            "url": "https://esl-api.retailit.lk/api/queue/{id}",
            "method": "PUT",
            "content_type": "application/json;charset=utf-8",
            "auth": "Bearer token. Admin, Manager or Operator.",
            "desc":
                "Moves the display period or changes the recurrence. Only the fields supplied are "
                "applied; all are optional except <code>userId</code>.",
            "params": [
                p("id", "Path", "Long", "Yes", "13", "Schedule identifier."),
                p("startDate", "Body", "DateTime", "No", '"2026-08-16T09:30:00"', "UTC."),
                p("endDate", "Body", "DateTime", "No", '"2026-08-16T14:30:00"', "UTC."),
                p("priorityId", "Body", "Int", "No", "2", "See Part 7.11."),
                p("isActive", "Body", "Boolean", "No", "true", "Retire or reinstate the schedule."),
                p("isRecurring", "Body", "Boolean", "No", "false", "Repeat the schedule."),
                p("recurrencePattern", "Body", "String", "No", "null", "Recurrence interval."),
                p("userId", "Body", "Int", "Yes", "42", "Identifier of the user making the change."),
            ],
            "demo": '''{
  "startDate": "2026-08-16T09:30:00",
  "endDate": "2026-08-16T14:30:00",
  "priorityId": 2,
  "isActive": true,
  "isRecurring": false,
  "recurrencePattern": null,
  "userId": 42
}''',
            "codes": CODES_STANDARD,
            "returns": '''{
  "success": true,
  "message": "Queue updated successfully",
  "responsCode": 200,
  "result": {
    "id": 13,
    "startDate": "2026-08-16T09:30:00Z",
    "endDate": "2026-08-16T14:30:00Z",
    "status": "Pending"
  }
}''',
        },
        {
            "name": "Run schedule immediately",
            "url": "https://esl-api.retailit.lk/api/queue/{id}/activate",
            "method": "POST",
            "content_type": "—",
            "auth": "Bearer token. Admin, Manager or Operator.",
            "desc":
                "Executes a schedule at once rather than waiting for its start time. The label is "
                "instructed to redraw immediately.",
            "notes": [
                "Do not use this on a schedule whose start time has already passed. The background "
                "service will have executed it within 30 seconds, and this call then returns "
                "<code>409</code>.",
                "Neither <code>409</code> nor <code>400</code> alters the schedule's recorded "
                "outcome, so a status of Failed always reflects a genuine execution attempt.",
            ],
            "params": [
                p("id", "Path", "Long", "Yes", "13", "Schedule identifier."),
            ],
            "demo": "POST /api/queue/13/activate",
            "codes": [
                ("200", "Executed. The label was instructed to redraw."),
                ("400", "The schedule's start time has not yet arrived."),
                ("404", "Unknown schedule."),
                ("409", "The schedule has already run."),
                ("500", "Execution failed. The schedule is recorded as Failed with the reason."),
            ],
            "returns": '''{
  "success": true,
  "message": "Queue activated successfully",
  "responsCode": 200,
  "result": {
    "id": 13,
    "status": "Completed",
    "lastAttempt": "2026-08-14T11:05:00Z"
  }
}''',
        },
        {
            "name": "Stop schedule",
            "url": "https://esl-api.retailit.lk/api/queue/{id}/deactivate",
            "method": "POST",
            "content_type": "—",
            "auth": "Bearer token. Admin, Manager or Operator.",
            "desc":
                "Retires a running schedule and restores the label's previous display.",
            "notes": [
                "The schedule remains <b>Completed</b> rather than reverting to Pending, so the "
                "history stays accurate. Its <code>isActive</code> flag is cleared, which is also "
                "what makes it eligible for deletion.",
                "Only a schedule that has run can be stopped. One still waiting for its start time "
                "has put nothing on a label, so this call answers <code>409</code> and changes "
                "nothing. Delete it instead, or let it run.",
            ],
            "params": [
                p("id", "Path", "Long", "Yes", "13", "Schedule identifier."),
            ],
            "demo": "POST /api/queue/13/deactivate",
            "codes": [
                ("200", "Stopped. The label's previous display is restored."),
                ("401", "Missing, expired or invalid bearer token."),
                ("403", "The signed-in user's role does not permit this operation."),
                ("404", "Unknown schedule."),
                ("409", "The schedule has not run, so there is nothing to stop."),
                ("500", "Unexpected server error."),
            ],
            "returns": '''{
  "success": true,
  "message": "Queue deactivated successfully",
  "responsCode": 200,
  "result": {
    "id": 13,
    "status": "Completed",
    "isActive": false
  }
}''',
        },
        {
            "name": "Delete schedule",
            "url": "https://esl-api.retailit.lk/api/queue/{id}",
            "method": "DELETE",
            "content_type": "—",
            "auth": "Bearer token. Admin, Manager or Operator.",
            "desc": "Removes a schedule.",
            "notes": [
                "A schedule still within its display period cannot be deleted. Stop it first using "
                "<b>Part 7.10</b>, then delete it.",
            ],
            "params": [
                p("id", "Path", "Long", "Yes", "13", "Schedule identifier."),
            ],
            "demo": "DELETE /api/queue/13",
            "codes": [
                ("200", "Deleted."),
                ("404", "Unknown schedule."),
                ("409", "The schedule is still displaying on its label. Stop it first."),
                ("500", "Unexpected server error."),
            ],
            "returns": '''{
  "success": true,
  "message": "Queue deleted successfully",
  "responsCode": 200
}''',
        },
        {
            "name": "Query priority list",
            "url": "https://esl-api.retailit.lk/api/queue/prioritytypes",
            "method": "GET",
            "content_type": "—",
            "auth": "Bearer token.",
            "desc":
                "The values accepted by <code>priorityId</code>. Priority determines which schedule "
                "wins when more than one targets the same label.",
            "params": [],
            "demo": "GET /api/queue/prioritytypes",
            "codes": CODES_READ,
            "returns": '''{
  "success": true,
  "message": "Priorities retrieved successfully",
  "responsCode": 200,
  "result": [
    { "id": 1, "priorityCode": "EMERGENCY",    "priorityName": "Emergency",    "value": 1 },
    { "id": 2, "priorityCode": "PRICE_CHANGE", "priorityName": "Price Change", "value": 2 },
    { "id": 3, "priorityCode": "PROMOTION",    "priorityName": "Promotion",    "value": 3 },
    { "id": 4, "priorityCode": "SCHEDULED",    "priorityName": "Scheduled",    "value": 4 },
    { "id": 5, "priorityCode": "MAINTENANCE",  "priorityName": "Maintenance",  "value": 5 }
  ]
}''',
        },
    ],
})

# ---------------------------------------------------------------------------
# Appendices
# ---------------------------------------------------------------------------

PARTS.append({
    "title": "Message API",
    "intro":
        "Messages are the pictures a label can display alongside its template — a promotion "
        "graphic, a seasonal banner, a shelf notice. A message is created once, then referenced "
        "by identifier when binding a label or scheduling one.",
    "prose": [
        ("Two kinds of message",
         "An <b>image</b> message is a file you upload. A <b>custom image</b> is composed in the "
         "portal's designer and arrives as a base64 string together with the canvas description "
         "that allows it to be reopened and edited. Both end up as a picture on the label; only "
         "the way they are supplied differs."),
        ("Match the message to the label's screen",
         "<code>screenSizeId</code> ties a message to a panel size, so a picture drawn for a "
         "296&times;128 label is not offered for a 400&times;300 one. Read the available sizes "
         "from <b>Part 3</b> and send the identifier that matches the label you intend to bind."),
        ("A message is not displayed until it is bound",
         "Creating a message stores it. It reaches a label only when that label is bound with the "
         "message, or when a schedule carrying it runs."),
    ],
    "endpoints": [
        {
            "name": "Query message list",
            "url": "https://esl-api.retailit.lk/api/message",
            "method": "GET",
            "content_type": "—",
            "auth": "Bearer token.",
            "desc":
                "Every active message for a store, newest first. Supplies the values used as "
                "<code>messageId</code> when binding or scheduling a label.",
            "params": [
                p("storeId", "Query", "Long", "No", "3", "Store to list messages for. Omit to list across stores."),
            ],
            "demo": "GET /api/message?storeId=3",
            "codes": CODES_READ,
            "returns": '''{
  "success": true,
  "message": "Messages retrieved successfully",
  "responsCode": 200,
  "result": [
    {
      "id": 12,
      "title": "Hot Chocolate",
      "contentType": 2,
      "duration": 5,
      "storeId": 3,
      "screenSizeId": 4,
      "isActive": true,
      "createdDate": "2026-02-11T09:14:22Z"
    }
  ]
}''',
        },
        {
            "name": "Query message list (paged)",
            "url": "https://esl-api.retailit.lk/api/message/paged",
            "method": "GET",
            "content_type": "—",
            "auth": "Bearer token.",
            "desc":
                "The same list with paging, sorting and filters, for a management screen rather "
                "than a lookup.",
            "params": [
                p("storeId", "Query", "Long", "No", "3", "Store to list messages for."),
                p("pageNumber", "Query", "Int", "No", "1", "Page number. Defaults to 1."),
                p("pageSize", "Query", "Int", "No", "10", "Rows per page. Defaults to 10."),
                p("title", "Query", "String", "No", '"Hot"', "Filters on message title."),
                p("contentType", "Query", "Int", "No", "2", "Restricts to one kind. See the table at the end of this part."),
                p("isActive", "Query", "Boolean", "No", "true", "Defaults to true."),
                p("sortBy", "Query", "String", "No", '"createdDate"', "Column to sort on."),
            ],
            "demo": "GET /api/message/paged?storeId=3&pageNumber=1&pageSize=10&contentType=2",
            "codes": CODES_READ,
            "returns": '''{
  "success": true,
  "message": "Messages retrieved successfully",
  "responsCode": 200,
  "result": {
    "items": [
      { "id": 12, "title": "Hot Chocolate", "contentType": 2, "duration": 5, "screenSizeId": 4, "isActive": true }
    ],
    "totalCount": 1,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 1
  }
}''',
        },
        {
            "name": "Query a message",
            "url": "https://esl-api.retailit.lk/api/message/{id}",
            "method": "GET",
            "content_type": "—",
            "auth": "Bearer token.",
            "desc":
                "One message including its content. Use this to retrieve the picture for preview, "
                "or the canvas description needed to reopen a custom image for editing.",
            "params": [
                p("id", "Path", "Long", "Yes", "12", "Message identifier."),
                p("storeId", "Query", "Long", "No", "3", "Restricts the lookup to one store."),
            ],
            "demo": "GET /api/message/12?storeId=3",
            "codes": CODES_READ,
            "returns": '''{
  "success": true,
  "message": "Message retrieved successfully",
  "responsCode": 200,
  "result": {
    "id": 12,
    "title": "Hot Chocolate",
    "contentType": 2,
    "contentData": "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQ...",
    "duration": 5,
    "storeId": 3,
    "screenSizeId": 4,
    "isActive": true
  }
}''',
        },
        {
            "name": "Query messages by identifier",
            "url": "https://esl-api.retailit.lk/api/message/messages/by-ids",
            "method": "POST",
            "content_type": "application/json;charset=utf-8",
            "auth": "Bearer token.",
            "desc":
                "Several messages in one call, given their identifiers. Intended for a screen that "
                "holds a list of message references and needs their titles and content together.",
            "params": [
                p("body", "Body", "Array", "Yes", "[12, 13]", "Message identifiers."),
                p("storeId", "Query", "Long", "No", "3", "Restricts the lookup to one store."),
            ],
            "demo": '''POST /api/message/messages/by-ids?storeId=3

[12, 13]''',
            "codes": CODES_READ,
            "returns": '''{
  "success": true,
  "message": "Messages retrieved successfully",
  "responsCode": 200,
  "result": [
    { "id": 12, "title": "Hot Chocolate", "contentType": 2, "screenSizeId": 4 },
    { "id": 13, "title": "Winter Offer",  "contentType": 4, "screenSizeId": 4 }
  ]
}''',
        },
        {
            "name": "Create an image message",
            "url": "https://esl-api.retailit.lk/api/message/image",
            "method": "POST",
            "content_type": "multipart/form-data",
            "auth": "Bearer token. Admin, Manager or Operator.",
            "desc":
                "Uploads a picture file and stores it as a message. The file is sent as form data "
                "rather than JSON, so the request is an ordinary multipart upload.",
            "notes": [
                "Prepare the picture at the label's own pixel size. An oversized file is stored as "
                "sent and slows every bind that carries it.",
                "<code>screenSizeId</code> should match the label the message is intended for; read "
                "the available sizes from <b>Part 3</b>.",
            ],
            "params": [
                p("image", "Form", "File", "Yes", "promo.jpg", "The picture to store."),
                p("title", "Form", "String", "Yes", '"Hot Chocolate"', "Display name for the message."),
                p("duration", "Form", "Int", "Yes", "5", "Seconds the picture is shown when part of a rotation."),
                p("createdBy", "Form", "Int", "Yes", "42", "Identifier of the user making the change."),
                p("storeId", "Form", "Long", "No", "3", "Store the message belongs to."),
                p("screenSizeId", "Form", "Long", "No", "4", "Panel size the picture was prepared for."),
            ],
            "demo": '''POST /api/message/image
Content-Type: multipart/form-data; boundary=----Boundary

------Boundary
Content-Disposition: form-data; name="image"; filename="promo.jpg"
Content-Type: image/jpeg

<binary image data>
------Boundary
Content-Disposition: form-data; name="title"

Hot Chocolate
------Boundary
Content-Disposition: form-data; name="duration"

5
------Boundary
Content-Disposition: form-data; name="createdBy"

42
------Boundary
Content-Disposition: form-data; name="storeId"

3
------Boundary
Content-Disposition: form-data; name="screenSizeId"

4
------Boundary--''',
            "codes": CODES_STANDARD,
            "returns": '''{
  "success": true,
  "message": "Image message uploaded successfully.",
  "responsCode": 200,
  "result": {
    "id": 12,
    "title": "Hot Chocolate",
    "contentType": 2,
    "duration": 5,
    "storeId": 3,
    "screenSizeId": 4,
    "isActive": true
  }
}''',
        },
        {
            "name": "Create a custom image message",
            "url": "https://esl-api.retailit.lk/api/message/custom-image",
            "method": "POST",
            "content_type": "application/json;charset=utf-8",
            "auth": "Bearer token. Admin, Manager or Operator.",
            "desc":
                "Stores a picture composed in the portal's designer. The rendered picture is sent as "
                "base64 and is what reaches the label; the canvas description is stored alongside it "
                "so the design can be reopened and amended later.",
            "notes": [
                "<code>imageData</code> is what reaches the label. <code>fabricJsData</code> is the "
                "editable description of the same design and is never sent to the label.",
                "Send <code>image_data</code> as a data URI, exactly as the designer produces it.",
                "This endpoint names its fields in snake_case — <code>image_data</code>, "
                "<code>fabric_js_data</code>, <code>created_by</code> — and <code>StoreId</code> "
                "with a capital S. <b>Replace a custom image message</b> uses camelCase "
                "instead. Send each exactly as shown.",
            ],
            "params": [
                p("title", "Body", "String", "Yes", '"Winter Offer"', "Display name for the message."),
                p("image_data", "Body", "String", "Yes", '"data:image/png;base64,iVBORw0..."', "Rendered picture, base64 encoded."),
                p("fabric_js_data", "Body", "String", "No", '"{...}"', "Canvas description, so the design can be reopened."),
                p("duration", "Body", "Int", "No", "5", "Seconds shown in a rotation. Defaults to 5."),
                p("created_by", "Body", "Int", "Yes", "42", "Identifier of the user making the change."),
                p("StoreId", "Body", "Long", "No", "3", "Store the message belongs to. Note the capital S."),
                p("screenSizeId", "Body", "Long", "No", "4", "Panel size the design was drawn for."),
            ],
            "demo": '''{
  "title": "Winter Offer",
  "image_data": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA...",
  "fabric_js_data": "{\\"version\\":\\"5.3.0\\",\\"objects\\":[...]}",
  "duration": 5,
  "created_by": 42,
  "StoreId": 3,
  "screenSizeId": 4
}''',
            "codes": CODES_STANDARD,
            "returns": '''{
  "success": true,
  "message": "Custom image message created successfully.",
  "responsCode": 200,
  "result": {
    "id": 13,
    "title": "Winter Offer",
    "contentType": 4,
    "duration": 5,
    "storeId": 3,
    "screenSizeId": 4,
    "isActive": true
  }
}''',
        },
        {
            "name": "Modify a message",
            "url": "https://esl-api.retailit.lk/api/message/{id}",
            "method": "PUT",
            "content_type": "application/json;charset=utf-8",
            "auth": "Bearer token. Admin, Manager or Operator.",
            "desc":
                "Amends a message's title, duration or active state without replacing the picture. "
                "Setting <code>isActive</code> to false retires it.",
            "notes": [
                "A label already displaying this message does not change until it is bound again or "
                "its schedule next runs.",
            ],
            "params": [
                p("id", "Path", "Long", "Yes", "12", "Message identifier."),
                p("title", "Body", "String", "No", '"Hot Chocolate"', "Display name."),
                p("contentData", "Body", "String", "No", '"data:image/jpeg;base64,..."', "Replacement picture, base64 encoded."),
                p("duration", "Body", "Int", "No", "8", "Seconds shown in a rotation."),
                p("isActive", "Body", "Boolean", "No", "true", "Set false to retire the message."),
                p("updatedUser", "Body", "Int", "Yes", "42", "Identifier of the user making the change."),
                p("storeId", "Body", "Long", "No", "3", "Store the message belongs to."),
                p("screenSizeId", "Body", "Long", "No", "4", "Panel size the picture was prepared for."),
            ],
            "demo": '''{
  "title": "Hot Chocolate",
  "duration": 8,
  "isActive": true,
  "updatedUser": 42,
  "storeId": 3,
  "screenSizeId": 4
}''',
            "codes": CODES_STANDARD,
            "returns": '''{
  "success": true,
  "message": "Message updated successfully",
  "responsCode": 200,
  "result": { "id": 12, "title": "Hot Chocolate", "duration": 8, "isActive": true }
}''',
        },
        {
            "name": "Replace an image message",
            "url": "https://esl-api.retailit.lk/api/message/image/{id}",
            "method": "PUT",
            "content_type": "multipart/form-data",
            "auth": "Bearer token. Admin, Manager or Operator.",
            "desc":
                "Uploads a new picture over an existing image message, keeping its identifier so "
                "every label and schedule already referencing it stays valid.",
            "params": [
                p("id", "Path", "Long", "Yes", "12", "Message identifier."),
                p("image", "Form", "File", "Yes", "promo-v2.jpg", "Replacement picture."),
                p("title", "Form", "String", "Yes", '"Hot Chocolate"', "Display name."),
                p("duration", "Form", "Int", "Yes", "5", "Seconds shown in a rotation."),
                p("updatedBy", "Form", "Int", "Yes", "42", "Identifier of the user making the change."),
                p("storeId", "Form", "Long", "No", "3", "Store the message belongs to."),
                p("screenSizeId", "Form", "Long", "No", "4", "Panel size the picture was prepared for."),
            ],
            "demo": '''PUT /api/message/image/12
Content-Type: multipart/form-data; boundary=----Boundary

------Boundary
Content-Disposition: form-data; name="image"; filename="promo-v2.jpg"
Content-Type: image/jpeg

<binary image data>
------Boundary
Content-Disposition: form-data; name="title"

Hot Chocolate
------Boundary
Content-Disposition: form-data; name="duration"

5
------Boundary
Content-Disposition: form-data; name="updatedBy"

42
------Boundary--''',
            "codes": CODES_STANDARD,
            "returns": '''{
  "success": true,
  "message": "Image message updated successfully.",
  "responsCode": 200,
  "result": { "id": 12, "title": "Hot Chocolate", "contentType": 2, "duration": 5 }
}''',
        },
        {
            "name": "Replace a custom image message",
            "url": "https://esl-api.retailit.lk/api/message/custom-image/{id}",
            "method": "PUT",
            "content_type": "application/json;charset=utf-8",
            "auth": "Bearer token. Admin, Manager or Operator.",
            "desc":
                "Stores an amended design over an existing custom image message, keeping its "
                "identifier.",
            "params": [
                p("id", "Path", "Long", "Yes", "13", "Message identifier."),
                p("title", "Body", "String", "Yes", '"Winter Offer"', "Display name."),
                p("imageData", "Body", "String", "Yes", '"data:image/png;base64,iVBORw0..."', "Re-rendered picture, base64 encoded."),
                p("fabricJsData", "Body", "String", "No", '"{...}"', "Amended canvas description."),
                p("duration", "Body", "Int", "No", "5", "Seconds shown in a rotation."),
                p("isActive", "Body", "Boolean", "No", "true", "Set false to retire the message."),
                p("updatedBy", "Body", "Int", "Yes", "42", "Identifier of the user making the change."),
            ],
            "demo": '''{
  "id": 13,
  "title": "Winter Offer",
  "imageData": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA...",
  "fabricJsData": "{\\"version\\":\\"5.3.0\\",\\"objects\\":[...]}",
  "duration": 5,
  "isActive": true,
  "updatedBy": 42
}''',
            "codes": CODES_STANDARD,
            "returns": '''{
  "success": true,
  "message": "Custom image message updated successfully.",
  "responsCode": 200,
  "result": { "id": 13, "title": "Winter Offer", "contentType": 4, "duration": 5 }
}''',
        },
        {
            "name": "Delete a message",
            "url": "https://esl-api.retailit.lk/api/message/{id}",
            "method": "DELETE",
            "content_type": "—",
            "auth": "Bearer token. Admin or Manager.",
            "desc":
                "Retires a message. The record is kept so that history referring to it stays "
                "readable; it stops appearing in the message list and can no longer be bound.",
            "notes": [
                "A label currently displaying the message keeps showing it until it is bound to "
                "something else — deleting the message does not clear the label.",
            ],
            "params": [
                p("id", "Path", "Long", "Yes", "12", "Message identifier."),
                p("userId", "Query", "Int", "Yes", "42", "Identifier of the user making the change."),
                p("storeId", "Query", "Long", "No", "3", "Store the message belongs to."),
            ],
            "demo": "DELETE /api/message/12?userId=42&storeId=3",
            "codes": CODES_STANDARD,
            "returns": '''{
  "success": true,
  "message": "Message deleted successfully",
  "responsCode": 200,
  "result": true
}''',
        },
    ],
    "tables": {
        "Message kinds": {
            "head": ["Value", "Kind", "Created by"],
            "rows": [
                ["<code>2</code>", "Image",
                 "<b>Create an image message</b> — a picture file uploaded as form data."],
                ["<code>4</code>", "Custom image",
                 "<b>Create a custom image message</b> — a design composed in the portal's designer."],
            ],
        },
    },
})

APPENDICES = [
    {
        "letter": "A",
        "title": "Response envelope",
        "intro":
            "Every endpoint in this document returns the same wrapper. Read <code>success</code> "
            "first; the payload is always under <code>result</code>.",
        "code": '''{
  "success": true,
  "message": "Product retrieved successfully.",
  "responsCode": 200,
  "error": null,
  "result": { }
}''',
        "table": {
            "head": ["Field", "Type", "Description"],
            "rows": [
                ["success", "Boolean", "True when the operation completed as requested. For bulk endpoints this is true only when every row succeeded."],
                ["message", "String", "Human-readable outcome. On failure this carries the reason."],
                ["responsCode", "Int", "Mirrors the HTTP status code."],
                ["error", "String", "Exception text on an unexpected server error; otherwise null."],
                ["result", "Object", "The payload. Shape varies by endpoint."],
            ],
        },
    },
    {
        "letter": "B",
        "title": "Status codes",
        "intro":
            "Codes used across the API. Endpoint-specific meanings are listed with each endpoint.",
        "table": {
            "head": ["Code", "Meaning", "Typical cause"],
            "rows": [
                ["200", "Success", "The request completed. For bulk endpoints, inspect the per-row results."],
                ["201", "Created", "A new product or schedule was created."],
                ["400", "Bad request", "A required field was missing, or a referenced record does not exist."],
                ["401", "Unauthorised", "No token, an expired token, or the email address was sent instead of the Employee ID."],
                ["403", "Forbidden", "The signed-in user holds the Viewer role, or the operation requires Admin or Manager."],
                ["404", "Not found", "No record matched. For product lookups, check that storeId is correct."],
                ["409", "Conflict", "The schedule's state does not allow the operation: an overlapping schedule exists, it has already run, it has not yet run, or it is still displaying."],
                ["500", "Server error", "Unexpected fault. <code>error</code> carries the detail."],
            ],
        },
    },
    {
        "letter": "C",
        "title": "Troubleshooting",
        "intro":
            "Conditions encountered most often during integration, and how to resolve them.",
        "table": {
            "head": ["Symptom", "Cause", "Resolution"],
            "rows": [
                ["<code>401</code> on log in", "The email address was sent as <code>userName</code>.",
                 "Send the Employee ID, for example <code>ADMIN01</code>."],
                ["<code>404</code> on a product that exists", "<code>storeId</code> is missing or refers to another store.",
                 "Product codes are unique per store. Supply the correct <code>storeId</code>."],
                ["<code>\"Category not found\"</code>", "The category name does not match an active category in that store.",
                 "Check against Part 3.1. Matching is exact."],
                ["<code>\"Product with same code already exists\"</code>", "Bulk <i>create</i> was used for a product already present.",
                 "Use <b>Part 5.2</b> for products that already exist."],
                ["<code>\"Store has no MinewStoreId\"</code>", "The store is not linked to a Minew cloud store.",
                 "Link the store once, then resend the affected rows. Until then products save to the database only."],
                ["Price accepted but the label did not change", "No label is bound to the product.",
                 "Supply <code>eslAssignments</code> when creating or updating. Confirm with <b>Part 4.6</b> — <code>hasEsl</code> should be true."],
                ["<code>eslBound: false</code> on a row that saved", "The cloud rejected the binding.",
                 "Usually a template belonging to another store, or one whose screen size does not match the label. Correct it and resend, or use <b>Part 6.3</b>."],
                ["A template identifier appears rounded", "It was parsed as a number and lost precision.",
                 "Template identifiers are strings. Keep them quoted throughout."],
                ["A schedule never runs", "<code>startDate</code> was sent in local time.",
                 "Convert to UTC by subtracting 5 hours 30 minutes. Verify with <b>Part 7.5</b>."],
                ["<code>409</code> when scheduling", "An unfinished schedule already covers that label, template and period.",
                 "Inspect existing schedules with <b>Part 7.7</b>, then amend or delete the conflicting one."],
                ["<code>409</code> when deleting a schedule", "It is still within its display period.",
                 "Stop it with <b>Part 7.10</b>, then delete."],
                ["A <code>comboId</code> is needed but only the label is known", "The pairing identifier is not carried by the label lookup.",
                 "Filter <b>Part 6.7</b> by <code>deviceId</code>, or send <b>Part 6.6</b> for the label and template — an existing pairing is returned rather than duplicated."],
                ["<code>409</code> when stopping a schedule", "It has not run yet, so nothing is displaying.",
                 "Only a schedule that has run can be stopped. Delete it with <b>Part 7.11</b>, or let it run first."],
            ],
        },
    },
]
