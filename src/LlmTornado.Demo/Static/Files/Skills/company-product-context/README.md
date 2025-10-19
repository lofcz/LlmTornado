# Company Product Context Compiler

An Anthropic skill for compiling comprehensive company product context from PDF documents, web research, and industry knowledge.

## Overview

This skill automates the process of extracting, analyzing, and synthesizing company information from multiple sources to create a comprehensive product context report. It combines automated PDF extraction with structured research templates and industry analysis frameworks.

## What It Does

- **Extracts** information from company PDF documents (annual reports, product sheets, presentations)
- **Analyzes** and structures data into key categories (products, business model, market position, etc.)
- **Synthesizes** information with web research and industry knowledge
- **Generates** comprehensive narrative reports and structured data outputs
- **Exports** deliverables in multiple formats for various use cases

## Quick Start

### Prerequisites

- Python 3.7+ with PyPDF2 library
- Company PDF documents
- Basic company information (name, website, industry)

### Basic Usage

1. **Place PDF files** in your input directory
2. **Run extraction**: `python3 extract_pdfs.py`
3. **Add research** (optional): Fill in web_research.md and industry_context.md templates
4. **Compile context**: `python3 compile_context.py`
5. **Export deliverables**: `bash export_deliverables.sh`

### Example Workflow

```bash
# Set up environment
export INPUT_DIR=/path/to/pdfs
export OUTPUT_DIR=/path/to/output

# Run the skill
python3 extract_pdfs.py
python3 compile_context.py
bash export_deliverables.sh
```

## Features

### Automated PDF Extraction

- Text extraction from all PDF pages
- Section identification (products, business model, strategy, etc.)
- Metadata extraction
- Metrics and entity recognition
- URL and contact information extraction

### Structured Data Organization

- Company profile compilation
- Product and service cataloging
- Business model documentation
- Market analysis aggregation
- Technology stack identification
- Customer information synthesis
- Strategic context capture
- Financial information extraction

### Comprehensive Reporting

- Professional narrative report (Markdown format)
- Structured data export (JSON format)
- Raw data preservation for reference
- Template-driven research guides
- Complete documentation

### Multiple Output Formats

- **Markdown Report**: Human-readable narrative format
- **JSON Data**: Machine-readable structured format
- **Templates**: Guided research input forms
- **Archive**: Complete package for sharing

## Use Cases

### Sales Enablement
- Competitive positioning research
- Product differentiation analysis
- Customer use case documentation
- Sales conversation preparation

### Strategic Planning
- Market opportunity assessment
- Competitive landscape analysis
- Strategic recommendation development
- Growth opportunity identification

### Investment Analysis
- Company due diligence
- Market position evaluation
- Technology stack assessment
- Risk and opportunity analysis

### Marketing & Content
- Competitive intelligence gathering
- Messaging and positioning development
- Content creation research
- Thought leadership foundation

### Business Development
- Partnership evaluation
- Acquisition target analysis
- Market entry research
- Ecosystem mapping

## Skills Components

### 1. extract_pdfs.py
Python script that extracts and structures information from PDF documents.

**Features:**
- Multi-page text extraction
- Section identification and categorization
- Entity and metric extraction
- URL and email discovery
- Metadata analysis

**Output:**
- Individual JSON files per PDF
- Aggregated company analysis
- Structured section data

### 2. compile_context.py
Python script that compiles extracted data into comprehensive context.

**Features:**
- Data aggregation across sources
- Section synthesis
- Narrative generation
- Gap identification
- Report formatting

**Output:**
- Narrative report (Markdown)
- Structured data (JSON)
- Processing summary

### 3. export_deliverables.sh
Bash script that packages all outputs for delivery.

**Features:**
- File organization
- Template generation
- Documentation creation
- Archive creation
- Summary generation

**Output:**
- Complete deliverables package
- Compressed archive
- README and documentation

### 4. SKILL.md
Complete workflow documentation with step-by-step instructions.

## File Structure

```
company-product-context/
├── SKILL.md                    # Complete skill documentation
├── README.md                   # This file
├── extract_pdfs.py            # PDF extraction script
├── compile_context.py         # Context compilation script
└── export_deliverables.sh     # Export packaging script
```

## Output Structure

```
company_context_deliverables/
├── reports/
│   ├── product_context_report.md    # Main narrative report
│   └── product_context.json         # Structured data
├── raw_data/
│   ├── [file]_extracted.json        # Individual extractions
│   ├── company_analysis.json        # Aggregated analysis
│   ├── web_research.md              # Research findings
│   └── industry_context.md          # Industry analysis
├── templates/
│   ├── web_research_template.md     # Web research guide
│   └── industry_context_template.md # Industry analysis guide
├── README.md                        # Usage documentation
└── SUMMARY.txt                      # Package summary
```

## Customization

### Adding Custom Sections

Edit `extract_pdfs.py` to add new section keywords:

```python
keywords = {
    'your_section': ['keyword1', 'keyword2', 'keyword3'],
    # ... existing sections
}
```

### Modifying Report Structure

Edit `compile_context.py` to customize report sections:

```python
def generate_narrative_report(context):
    report = f"""
    # Your Custom Section
    {your_content}
    """
    return report
```

### Custom Metrics Extraction

Add patterns in `extract_pdfs.py`:

```python
metrics_patterns = [
    r'your_pattern_here',
    # ... existing patterns
]
```

## Tips for Best Results

### Document Preparation
- ✓ Use text-based PDFs (not scanned images)
- ✓ Include diverse document types (reports, presentations, docs)
- ✓ Ensure documents are recent and relevant
- ✓ Remove password protection from PDFs

### Research Enhancement
- ✓ Use provided templates for structured research
- ✓ Include official sources (website, LinkedIn, docs)
- ✓ Add third-party analysis (Gartner, Forrester, etc.)
- ✓ Document customer reviews and testimonials
- ✓ Include recent news and press releases

### Analysis Improvement
- ✓ Apply domain expertise to findings
- ✓ Validate extracted metrics
- ✓ Add competitive context
- ✓ Include strategic implications
- ✓ Provide actionable recommendations

### Report Quality
- ✓ Fill in all [bracketed] placeholders
- ✓ Add section-specific insights
- ✓ Include visual descriptions where helpful
- ✓ Ensure consistency across sections
- ✓ Proofread for clarity and accuracy

## Troubleshooting

### PDF Extraction Issues

**Problem**: No text extracted from PDF
- **Cause**: Scanned image-based PDF
- **Solution**: Use OCR preprocessing or different source document

**Problem**: Garbled or incorrect text
- **Cause**: PDF encoding issues
- **Solution**: Convert PDF to different format and re-export

### Section Identification

**Problem**: Content not categorized correctly
- **Cause**: Non-standard section headers
- **Solution**: Customize section keywords in extract_pdfs.py

### Missing Information

**Problem**: Report has many empty sections
- **Cause**: Limited source documents
- **Solution**: Add more PDFs or manually complete with research

### Data Accuracy

**Problem**: Extracted metrics are incorrect
- **Cause**: Pattern matching limitations
- **Solution**: Manually verify and correct metrics

## Requirements

### Python Libraries
```bash
pip install PyPDF2
```

### System Requirements
- Python 3.7 or higher
- Bash shell (Linux, macOS, WSL on Windows)
- Sufficient disk space for PDF processing

### Optional
- Markdown viewer for report review
- JSON viewer/editor for data inspection
- Git for version control

## Integration

### CRM Systems
Import the JSON data into Salesforce, HubSpot, or other CRM platforms using their APIs or data import tools.

### Competitive Intelligence Platforms
Use structured data as input for competitive intelligence databases and analysis tools.

### Knowledge Management
Store reports in confluence, Notion, or SharePoint for team access and collaboration.

### Analytics Platforms
Parse JSON data for custom dashboards and analytics using BI tools.

## Best Practices

### Documentation
- Keep source PDFs organized and versioned
- Document extraction date and source details
- Maintain change log for report updates
- Archive previous versions

### Data Quality
- Verify information from multiple sources
- Cross-reference extracted metrics
- Update regularly as company evolves
- Include data quality notes

### Collaboration
- Share templates with research team
- Use version control for reports
- Establish review process
- Define update cadence

### Security
- Handle confidential information appropriately
- Use access controls for sensitive data
- Clean up temporary files after export
- Secure storage for deliverables

## Advanced Usage

### Batch Processing
Process multiple companies:

```bash
for company in company1 company2 company3; do
    export INPUT_DIR=/data/$company
    python3 extract_pdfs.py
    python3 compile_context.py
    bash export_deliverables.sh
done
```

### Automated Updates
Set up scheduled extraction:

```bash
# Add to crontab for weekly updates
0 0 * * 0 /path/to/company-product-context/extract_pdfs.py
```

### Custom Workflows
Create your own pipeline:

```python
# custom_workflow.py
import extract_pdfs
import compile_context

# Your custom processing
```

## Contributing

Suggestions for improvement:
- Additional extraction patterns
- New report sections
- Enhanced analysis frameworks
- Integration examples
- Template improvements

## Version History

- **v1.0** (2024) - Initial release
  - PDF extraction
  - Context compilation
  - Report generation
  - Export packaging

## License

This skill is part of the Anthropic Skills library.

## Support

For issues or questions:
1. Review the SKILL.md documentation
2. Check troubleshooting section
3. Examine example outputs
4. Consult skill creator

## Acknowledgments

Built for the Anthropic Skills ecosystem to enable comprehensive company analysis and product context development.

---

**Skill Name**: company-product-context  
**Version**: 1.0  
**Category**: Business Intelligence, Research, Analysis  
**Complexity**: Intermediate  
**Estimated Time**: 15-30 minutes per company
