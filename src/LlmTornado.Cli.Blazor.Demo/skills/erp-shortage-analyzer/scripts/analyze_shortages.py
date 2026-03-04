"""
Shortage Analysis Script

Input:  JSON array of shortage items (via stdin or argument)
Output: Prioritized action plan (printed to stdout)

Expected shortage item format:
{
    "partId": "MLCC-0402",
    "description": "MLCC 0402 100nF",
    "shortageQty": 5000,
    "onHandQty": 200,
    "preferredVendor": "ACME-001",
    "leadTimeDays": 84,
    "unitCost": 0.015
}
"""

import json
import sys


def analyze_shortages(items: list[dict]) -> dict:
    """Analyze shortage items and produce a prioritized action plan."""

    # Calculate criticality score: shortage_qty * unit_cost * lead_time_factor
    for item in items:
        shortage = item.get("shortageQty", 0)
        cost = item.get("unitCost", 0)
        lead_days = item.get("leadTimeDays", 0)
        lead_factor = 1.5 if lead_days > 56 else 1.0  # 8 weeks = 56 days

        item["criticalityScore"] = round(shortage * cost * lead_factor, 2)
        item["isLongLead"] = lead_days > 56
        item["isEscalation"] = lead_days > 84  # 12 weeks

    # Sort by criticality (highest first)
    items.sort(key=lambda x: x["criticalityScore"], reverse=True)

    # Group by vendor
    vendor_groups = {}
    no_vendor = []
    for item in items:
        if item.get("shortageQty", 0) <= 0:
            continue
        vendor = item.get("preferredVendor")
        if not vendor:
            no_vendor.append(item)
        else:
            vendor_groups.setdefault(vendor, []).append(item)

    # Build action plan
    action_plan = {
        "totalItems": len(items),
        "criticalItems": len([i for i in items if i.get("isLongLead")]),
        "escalationItems": len([i for i in items if i.get("isEscalation")]),
        "noVendorItems": len(no_vendor),
        "vendorGroups": {
            vendor: {
                "itemCount": len(group),
                "totalCost": round(sum(
                    i["shortageQty"] * i.get("unitCost", 0) for i in group
                ), 2),
                "items": [
                    {
                        "partId": i["partId"],
                        "description": i.get("description", ""),
                        "shortageQty": i["shortageQty"],
                        "criticalityScore": i["criticalityScore"],
                        "isLongLead": i["isLongLead"],
                    }
                    for i in group
                ],
            }
            for vendor, group in vendor_groups.items()
        },
        "noVendorItems_detail": [
            {"partId": i["partId"], "shortageQty": i["shortageQty"]}
            for i in no_vendor
        ],
    }

    return action_plan


if __name__ == "__main__":
    # Read from stdin
    raw = sys.stdin.read().strip()
    if not raw:
        print(json.dumps({"error": "No input provided. Pass shortage JSON via stdin."}))
        sys.exit(1)

    try:
        data = json.loads(raw)
    except json.JSONDecodeError as e:
        print(json.dumps({"error": f"Invalid JSON: {e}"}))
        sys.exit(1)

    result = analyze_shortages(data)
    print(json.dumps(result, indent=2))
