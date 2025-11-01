# Skill Creator Examples

Real-world examples of using the Skill Creator skill.

## Table of Contents

1. [Basic Skills](#basic-skills)
2. [Data Processing Skills](#data-processing-skills)
3. [API Integration Skills](#api-integration-skills)
4. [Automation Skills](#automation-skills)
5. [Analysis Skills](#analysis-skills)

---

## Basic Skills

### Example 1: File Organizer

**Request**:
```
Create a skill called "file-organizer" that organizes files by type into folders
```

**Generated Structure**:
```
file-organizer/
├── SKILL.md
└── README.md

Steps created:
1. Scan directory for files
2. Identify file types by extension
3. Create category folders
4. Move files to appropriate folders
5. Generate organization report
```

---

### Example 2: Text Formatter

**Request**:
```
Create a skill called "text-formatter" that formats text files with these steps:
- Remove extra whitespace
- Fix line endings
- Apply consistent indentation
- Add proper headers
```

**Result**: Complete skill with validation and formatting logic

---

## Data Processing Skills

### Example 3: CSV Analyzer

**Request**:
```
Create a skill called "csv-analyzer" that:
1. Loads CSV files
2. Validates data integrity
3. Generates statistical summary
4. Creates visualizations
5. Exports report

Include Python scripts for data processing.
```

**Generated Files**:
```
csv-analyzer/
├── SKILL.md
├── README.md
└── scripts/
    ├── load_csv.py
    ├── validate_data.py
    ├── generate_stats.py
    └── create_visualizations.py
```

---

### Example 4: Data Cleaner

**Request**:
```
Create a "data-cleaner" skill for cleaning messy datasets with:
- Remove duplicates
- Handle missing values
- Normalize formats
- Validate data types
- Export cleaned data
```

**Features**:
- Progress tracking
- Validation rules
- Export options
- Error handling

---

## API Integration Skills

### Example 5: REST API Client

**Request**:
```
Create an "api-client" skill that integrates with REST APIs:

Steps:
1. Setup: Configure API credentials (key, secret, endpoint)
2. Authenticate: Handle OAuth or API key authentication
3. Request: Send GET/POST/PUT/DELETE requests
4. Error Handling: Retry logic and error messages
5. Response Processing: Parse JSON and extract data
6. Export: Save results to file

Include Python script for API interactions.
```

**Generated Script** (`scripts/api_client.py`):
```python
import requests
import os

class APIClient:
    def __init__(self, base_url, api_key):
        self.base_url = base_url
        self.headers = {"Authorization": f"Bearer {api_key}"}
    
    def get(self, endpoint):
        # Implementation
        pass
    
    def post(self, endpoint, data):
        # Implementation
        pass
```

---

### Example 6: Webhook Handler

**Request**:
```
Create a "webhook-handler" skill that:
- Receives webhook events
- Validates payloads
- Processes events
- Triggers actions
- Logs activity
```

---

## Automation Skills

### Example 7: Backup Manager

**Request**:
```
Create a "backup-manager" skill for automated backups:

Workflow:
1. Identify source directories
2. Create timestamped backup folder
3. Copy files with compression
4. Verify backup integrity
5. Clean up old backups (keep last 5)
6. Send notification

Include bash script for automation.
```

**Generated Script** (`scripts/backup.sh`):
```bash
#!/bin/bash
BACKUP_DIR="/backups/$(date +%Y%m%d_%H%M%S)"
mkdir -p "$BACKUP_DIR"
tar -czf "$BACKUP_DIR/backup.tar.gz" /source/path
echo "✓ Backup completed: $BACKUP_DIR"
```

---

### Example 8: Report Generator

**Request**:
```
Create a "report-generator" skill that generates automated reports:
- Collect data from multiple sources
- Process and analyze data
- Create charts and graphs
- Generate PDF report
- Email to stakeholders
```

---

## Analysis Skills

### Example 9: Log Analyzer

**Request**:
```
Create a "log-analyzer" skill for analyzing application logs:

Steps:
1. Load log files
2. Parse log entries
3. Filter by severity level
4. Identify error patterns
5. Generate statistics
6. Create summary report
7. Highlight critical issues

Use Python for complex parsing.
```

---

### Example 10: Performance Monitor

**Request**:
```
Create a "performance-monitor" skill that:
- Monitors system metrics (CPU, memory, disk)
- Tracks application performance
- Detects anomalies
- Generates alerts
- Creates performance dashboard
```

---

## Complex Multi-Step Skills

### Example 11: Complete ML Pipeline

**Request**:
```
Create an "ml-pipeline" skill for machine learning workflows:

Workflow:
1. Data Ingestion: Load data from multiple sources (CSV, API, database)
2. Data Validation: Check schema, types, ranges
3. Data Cleaning: Handle missing values, outliers
4. Feature Engineering: Create features, encode categories
5. Train-Test Split: Stratified split with validation set
6. Model Training: Train multiple models (RF, XGBoost, NN)
7. Hyperparameter Tuning: Grid search with cross-validation
8. Evaluation: Generate metrics, confusion matrix, ROC curves
9. Model Selection: Choose best performing model
10. Export Model: Save model with metadata
11. Generate Report: PDF with all results and visualizations

Include Python scripts for each major step.
```

**Result**: Complete ML pipeline skill with:
- 11 detailed workflow steps
- Python scripts for each component
- Configuration templates
- Comprehensive documentation
- Example notebooks

---

### Example 12: CI/CD Pipeline

**Request**:
```
Create a "cicd-pipeline" skill for continuous integration/deployment:

Stages:
1. Code Checkout: Clone repository and checkout branch
2. Dependency Installation: Install required packages
3. Linting: Run code quality checks
4. Unit Tests: Execute test suite
5. Integration Tests: Test component interactions
6. Build: Create production build
7. Security Scan: Check for vulnerabilities
8. Deploy to Staging: Deploy to test environment
9. Smoke Tests: Verify deployment
10. Deploy to Production: Production deployment
11. Monitoring: Set up alerts and monitoring

Include configuration files for popular CI/CD tools.
```

---

## Customization Examples

### Example 13: Skill with Custom Configuration

**Request**:
```
Create a "custom-processor" skill with:
- Configurable settings via JSON
- Multiple operation modes
- Plugin architecture
- Extensive logging

Configuration file structure:
{
  "mode": "batch",
  "input_dir": "/data/input",
  "output_dir": "/data/output",
  "options": {
    "parallel": true,
    "max_workers": 4
  }
}
```

---

### Example 14: Interactive Skill

**Request**:
```
Create an "interactive-wizard" skill that:
- Prompts user for input at each step
- Validates input before proceeding
- Allows going back to previous steps
- Saves progress for resuming later
- Provides helpful hints and examples
```

---

## GitHub Repository Integration Examples

### Example 15: Direct Repository Upload

**Request**:
```
Create a skill called "code-reviewer" and upload it to my repository "johndoe/Agent-Skills"

Use GitHub CLI authentication.
```

**Process**:
1. ✅ Skill generated: `/tmp/code-reviewer/`
2. ✅ Branch created: `add-code-reviewer-skill`
3. ✅ Files committed and pushed
4. ✅ PR created: `https://github.com/johndoe/Agent-Skills/pull/123`
5. ✅ Files downloaded to PC

---

### Example 16: Multiple Skills at Once

**Request**:
```
Create three related skills:
1. "data-loader" - Loads data from various sources
2. "data-transformer" - Transforms and enriches data
3. "data-exporter" - Exports to different formats

Create separate PRs for each skill.
```

---

## Tips for Requesting Skills

### Be Specific

❌ **Vague**: "Create a data skill"

✅ **Specific**: "Create a 'csv-merger' skill that combines multiple CSV files with the same schema, removes duplicates, and exports the result"

### Provide Structure

❌ **Unstructured**: "I need something for APIs"

✅ **Structured**:
```
Create an "api-monitor" skill with these steps:
1. Setup: Configure API endpoints to monitor
2. Health Check: Ping each endpoint
3. Response Time: Measure latency
4. Status Tracking: Log status codes
5. Alerting: Send notification on failures
6. Reporting: Generate uptime report
```

### Include Requirements

❌ **No requirements**: "Create a backup skill"

✅ **With requirements**:
```
Create a "cloud-backup" skill that:
- Supports AWS S3 and Google Cloud Storage
- Encrypts data before upload
- Handles incremental backups
- Includes Python script for cloud operations
- Requires boto3 for AWS
```

### Specify Outputs

❌ **No output spec**: "Create an analysis skill"

✅ **With outputs**:
```
Create a "sales-analyzer" skill that outputs:
- CSV with daily sales summary
- JSON with trend analysis
- PNG charts (revenue over time, top products)
- PDF report with key insights
```

---

## Testing Your Generated Skills

After a skill is generated, test it:

```bash
# Validate structure
python3 /path/to/skill-creator/scripts/validate_skill.py /tmp/your-skill

# Check files
ls -la /tmp/your-skill/

# Review SKILL.md
cat /tmp/your-skill/SKILL.md

# Test any scripts
python3 /tmp/your-skill/scripts/helper.py --help
```

---

## Contributing Your Skills

After creating and testing a skill:

1. ✅ Review the generated PR
2. ✅ Make any necessary adjustments
3. ✅ Request review from team members
4. ✅ Merge the PR
5. ✅ Share with the community!

---

## Need More Examples?

Check out the existing skills in the repository for inspiration:
- Browse: `https://github.com/yourusername/Agent-Skills/tree/main/skills`
- Clone: `git clone https://github.com/yourusername/Agent-Skills.git`
- Explore: `cd Agent-Skills/skills && ls`

---

**Happy Skill Creating!** 🚀

For more information, see:
- [SKILL.md](SKILL.md) - Complete workflow
- [README.md](README.md) - Overview and features
- [INSTALL.md](INSTALL.md) - Installation guide
