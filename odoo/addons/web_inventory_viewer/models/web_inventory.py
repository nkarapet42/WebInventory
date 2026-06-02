from odoo import fields, models


class WebInventory(models.Model):
    _name = "web.inventory"
    _description = "Imported WebInventory"
    _order = "title"

    title = fields.Char(required=True, readonly=True)
    external_inventory_id = fields.Char(required=True, readonly=True, index=True)
    item_count = fields.Integer(readonly=True)
    source_updated_at = fields.Datetime(readonly=True)
    imported_at = fields.Datetime(readonly=True)
    field_ids = fields.One2many("web.inventory.field", "inventory_id", readonly=True)

    _sql_constraints = [
        (
            "external_inventory_id_unique",
            "unique(external_inventory_id)",
            "This WebInventory inventory has already been imported.",
        ),
    ]


class WebInventoryField(models.Model):
    _name = "web.inventory.field"
    _description = "Imported WebInventory Field"
    _order = "id"

    inventory_id = fields.Many2one("web.inventory", required=True, ondelete="cascade", readonly=True)
    title = fields.Char(required=True, readonly=True)
    field_type = fields.Char(required=True, readonly=True)
    filled_count = fields.Integer(readonly=True)
    average = fields.Float(readonly=True)
    minimum = fields.Float(readonly=True)
    maximum = fields.Float(readonly=True)
    true_count = fields.Integer(readonly=True)
    false_count = fields.Integer(readonly=True)
    top_value_ids = fields.One2many("web.inventory.value", "field_id", readonly=True)


class WebInventoryValue(models.Model):
    _name = "web.inventory.value"
    _description = "Imported WebInventory Frequent Value"
    _order = "count desc, value"

    field_id = fields.Many2one("web.inventory.field", required=True, ondelete="cascade", readonly=True)
    value = fields.Char(required=True, readonly=True)
    count = fields.Integer(readonly=True)
