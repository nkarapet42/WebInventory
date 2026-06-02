{
    "name": "WebInventory Viewer",
    "version": "18.0.1.0.0",
    "category": "Inventory",
    "summary": "Read-only viewer for imported WebInventory aggregates",
    "depends": ["base"],
    "data": [
        "security/ir.model.access.csv",
        "views/web_inventory_views.xml",
        "wizard/web_inventory_import_wizard_views.xml",
    ],
    "application": True,
    "installable": True,
    "license": "LGPL-3",
}
