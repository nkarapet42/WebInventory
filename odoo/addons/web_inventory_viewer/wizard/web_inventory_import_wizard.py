import json
from datetime import datetime, timezone
from urllib.error import HTTPError, URLError
from urllib.parse import urlparse
from urllib.request import Request, urlopen

from odoo import _, fields, models
from odoo.exceptions import UserError


class WebInventoryImportWizard(models.TransientModel):
    _name = "web.inventory.import.wizard"
    _description = "Import WebInventory Aggregates"

    api_url = fields.Char(
        required=True,
        default="http://web:8080/api/inventories/aggregates",
        help="Use http://web:8080/api/inventories/aggregates when both applications run in this Docker Compose stack.",
    )
    api_token = fields.Char(required=True)

    def action_import(self):
        self.ensure_one()
        payload = self._fetch_payload()
        inventory_id = str(payload.get("inventoryId") or "").strip()
        title = str(payload.get("title") or "").strip()
        if not inventory_id or not title or not isinstance(payload.get("fields"), list):
            raise UserError(_("WebInventory returned an invalid aggregate payload."))

        field_commands = [(5, 0, 0)]
        for source_field in payload["fields"]:
            field_commands.append((0, 0, self._field_values(source_field)))

        inventory_model = self.env["web.inventory"].sudo()
        inventory = inventory_model.search([("external_inventory_id", "=", inventory_id)], limit=1)
        values = {
            "title": title,
            "external_inventory_id": inventory_id,
            "item_count": int(payload.get("itemCount") or 0),
            "source_updated_at": self._parse_datetime(payload.get("updatedAt")),
            "imported_at": fields.Datetime.now(),
            "field_ids": field_commands,
        }
        if inventory:
            inventory.write(values)
        else:
            inventory = inventory_model.create(values)

        return {
            "type": "ir.actions.act_window",
            "name": _("Imported Inventory"),
            "res_model": "web.inventory",
            "res_id": inventory.id,
            "view_mode": "form",
            "target": "current",
        }

    def _fetch_payload(self):
        url = self.api_url.strip()
        parsed = urlparse(url)
        if parsed.scheme not in ("http", "https") or not parsed.netloc:
            raise UserError(_("Enter a valid HTTP or HTTPS WebInventory API URL."))

        request = Request(
            url,
            headers={
                "Authorization": f"Bearer {self.api_token.strip()}",
                "Accept": "application/json",
            },
        )
        try:
            with urlopen(request, timeout=10) as response:
                return json.load(response)
        except HTTPError as error:
            if error.code == 401:
                raise UserError(_("WebInventory rejected the API token.")) from error
            raise UserError(_("WebInventory API returned HTTP status %s.") % error.code) from error
        except (URLError, TimeoutError, json.JSONDecodeError) as error:
            raise UserError(_("Could not read WebInventory aggregate data: %s") % error) from error

    @staticmethod
    def _field_values(source_field):
        if not isinstance(source_field, dict):
            raise UserError(_("WebInventory returned an invalid field aggregate."))

        return {
            "title": str(source_field.get("title") or ""),
            "field_type": str(source_field.get("type") or ""),
            "filled_count": int(source_field.get("filledCount") or 0),
            "average": source_field.get("average"),
            "minimum": source_field.get("minimum"),
            "maximum": source_field.get("maximum"),
            "true_count": source_field.get("trueCount"),
            "false_count": source_field.get("falseCount"),
            "top_value_ids": [
                (
                    0,
                    0,
                    {
                        "value": str(top_value.get("value") or ""),
                        "count": int(top_value.get("count") or 0),
                    },
                )
                for top_value in source_field.get("topValues") or []
            ],
        }

    @staticmethod
    def _parse_datetime(value):
        if not value:
            return False

        parsed = datetime.fromisoformat(str(value).replace("Z", "+00:00"))
        if parsed.tzinfo is not None:
            parsed = parsed.astimezone(timezone.utc).replace(tzinfo=None)
        return parsed
