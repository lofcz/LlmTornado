---
name: erp-shortage-analyzer
description: >
  Analyzes purchase shortages from the Visual ERP system, identifies critical items
  by lead time and cost impact, and generates RFQ recommendations. Use when the user
  asks about shortages, procurement priorities, or RFQ planning.
license: Proprietary
category: purchasing
related-skills: []
metadata:
  author: visual-erp-team
  version: "1.0"
---

# ERP Shortage Analyzer

## Purpose
This skill helps you analyze purchase shortages and create actionable procurement plans.

## Instructions

### Step 1: Retrieve Shortages
Use the `get_purchase_shortages` ERP tool to retrieve the current shortage report.
Optionally filter by site ID (default: all sites) and horizon days (default: 90).

### Step 2: Analyze with Python Script
Read and execute `scripts/analyze_shortages.py` to run the analysis.
Pass the shortage data as a JSON string argument.

The script will:
- Sort shortages by criticality (based on shortage quantity x unit cost)
- Group by preferred vendor
- Flag items with lead times > 8 weeks
- Output a prioritized action plan

### Step 3: Generate RFQ Recommendations
Based on the analysis output, recommend RFQs grouped by vendor.
For each RFQ line, include quantity breaks at 1x, 1.5x, 2x, and 3x the shortage quantity.

### Step 4: Review Reference Guide
See [references/SHORTAGE-GUIDE.md](references/SHORTAGE-GUIDE.md) for the team's
procurement best practices and approval thresholds.

### Step 5: Use the RFQ Template
The CSV template at [assets/rfq-template.csv](assets/rfq-template.csv) shows the
expected format for bulk RFQ imports.

## Common Edge Cases
- **Zero shortage quantity**: Skip — these are informational only.
- **No preferred vendor**: Flag for manual assignment.
- **Lead time > 12 weeks**: Escalate to procurement manager.
