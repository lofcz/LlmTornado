---
name: company-product-context
description: Compiles comprehensive company product context from PDF documents, web research, and industry knowledge
---

## Company Product Context Compiler

This skill extracts information from company PDF documents, conducts web research, and synthesizes industry knowledge to create a comprehensive company product context report.

Copy this checklist and track your progress:

```
Company Product Context Progress:
- [ ] Step 1: Gather company materials and identify sources
- [ ] Step 2: Extract information from PDF documents
- [ ] Step 3: Structure extracted data
- [ ] Step 4: Conduct web research and validation
- [ ] Step 5: Synthesize industry knowledge
- [ ] Step 6: Compile comprehensive product context
- [ ] Step 7: Generate final report
- [ ] Step 8: Export deliverables
```

## **Step 1: Gather company materials and identify sources**

Collect all available company information:

**Required Inputs:**
- Company PDF documents (annual reports, product sheets, presentations, etc.)
- Company name and website URL
- Industry/sector information
- Specific products or services to focus on (if applicable)

**Actions:**
1. Request all relevant PDF files from user
2. Confirm company name, website, and primary industry
3. Ask about specific focus areas or products of interest
4. Identify any competitive context needed

**Expected in INPUT_DIR:**
- `*.pdf` - Company documents
- `company_info.txt` - Basic company details (optional)

## **Step 2: Extract information from PDF documents**

Extract structured information from all provided PDF files.

**Use the Python script for PDF extraction:**

```python
import os
import re
from pathlib import Path
import PyPDF2
import json

def extract_pdf_content(pdf_path):
    """Extract text content from PDF file."""
    text_content = []
    metadata = {}
    
    try:
        with open(pdf_path, 'rb') as file:
            pdf_reader = PyPDF2.PdfReader(file)
            
            # Extract metadata
            if pdf_reader.metadata:
                metadata = {
                    'title': pdf_reader.metadata.get('/Title', ''),
                    'author': pdf_reader.metadata.get('/Author', ''),
                    'subject': pdf_reader.metadata.get('/Subject', ''),
                    'pages': len(pdf_reader.pages)
                }
            else:
                metadata = {'pages': len(pdf_reader.pages)}
            
            # Extract text from all pages
            for page_num, page in enumerate(pdf_reader.pages, 1):
                try:
                    text = page.extract_text()
                    if text.strip():
                        text_content.append({
                            'page': page_num,
                            'text': text
                        })
                except Exception as e:
                    print(f"Error extracting page {page_num}: {e}")
                    
    except Exception as e:
        print(f"Error reading PDF {pdf_path}: {e}")
        return None
    
    return {
        'filename': os.path.basename(pdf_path),
        'metadata': metadata,
        'content': text_content
    }

def extract_key_sections(text):
    """Extract key sections from text based on common headers."""
    sections = {
        'company_overview': [],
        'products_services': [],
        'business_model': [],
        'market_position': [],
        'financials': [],
        'technology': [],
        'customers': [],
        'strategy': [],
        'other': []
    }
    
    # Keywords for section identification
    keywords = {
        'company_overview': ['about us', 'company overview', 'who we are', 'introduction', 'history'],
        'products_services': ['products', 'services', 'solutions', 'offerings', 'portfolio'],
        'business_model': ['business model', 'revenue model', 'how we work', 'operations'],
        'market_position': ['market', 'industry', 'competitive', 'position', 'landscape'],
        'financials': ['financial', 'revenue', 'earnings', 'profit', 'growth'],
        'technology': ['technology', 'platform', 'infrastructure', 'technical', 'innovation'],
        'customers': ['customers', 'clients', 'partners', 'case study', 'testimonial'],
        'strategy': ['strategy', 'vision', 'mission', 'goals', 'objectives', 'roadmap']
    }
    
    lines = text.split('\n')
    current_section = 'other'
    
    for line in lines:
        line_lower = line.lower().strip()
        
        # Check if line is a section header
        for section, section_keywords in keywords.items():
            if any(keyword in line_lower for keyword in section_keywords):
                if len(line_lower) < 100:  # Likely a header
                    current_section = section
                    break
        
        if line.strip():
            sections[current_section].append(line)
    
    return sections

def analyze_company_info(extracted_data):
    """Analyze extracted data for key company information."""
    analysis = {
        'company_name': '',
        'industry': '',
        'products': [],
        'key_terms': [],
        'metrics': [],
        'urls': [],
        'emails': []
    }
    
    all_text = ''
    for doc in extracted_data:
        for page in doc['content']:
            all_text += page['text'] + '\n'
    
    # Extract URLs
    url_pattern = r'http[s]?://(?:[a-zA-Z]|[0-9]|[$-_@.&+]|[!*\\(\\),]|(?:%[0-9a-fA-F][0-9a-fA-F]))+'
    analysis['urls'] = list(set(re.findall(url_pattern, all_text)))
    
    # Extract emails
    email_pattern = r'\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b'
    analysis['emails'] = list(set(re.findall(email_pattern, all_text)))
    
    # Extract potential metrics (numbers with units/context)
    metrics_pattern = r'\$?\d+\.?\d*\s*(?:million|billion|trillion|k|M|B|%|percent|users|customers|employees)'
    analysis['metrics'] = re.findall(metrics_pattern, all_text, re.IGNORECASE)
    
    return analysis

def main():
    input_dir = os.environ.get('INPUT_DIR', '/tmp')
    output_dir = '/tmp/extracted_data'
    os.makedirs(output_dir, exist_ok=True)
    
    # Find all PDF files
    pdf_files = list(Path(input_dir).glob('*.pdf'))
    
    if not pdf_files:
        print("No PDF files found in input directory")
        return
    
    print(f"Found {len(pdf_files)} PDF file(s)")
    
    extracted_data = []
    
    for pdf_file in pdf_files:
        print(f"\nProcessing: {pdf_file.name}")
        data = extract_pdf_content(str(pdf_file))
        
        if data:
            extracted_data.append(data)
            
            # Extract sections from content
            all_text = '\n'.join([page['text'] for page in data['content']])
            sections = extract_key_sections(all_text)
            
            # Save individual file data
            output_file = output_dir + f"/{pdf_file.stem}_extracted.json"
            with open(output_file, 'w', encoding='utf-8') as f:
                json.dump({
                    'metadata': data['metadata'],
                    'sections': {k: '\n'.join(v) for k, v in sections.items() if v},
                    'full_text': all_text
                }, f, indent=2, ensure_ascii=False)
            
            print(f"✓ Extracted {len(data['content'])} pages")
            print(f"✓ Saved to: {output_file}")
    
    # Analyze all extracted data
    if extracted_data:
        analysis = analyze_company_info(extracted_data)
        
        analysis_file = output_dir + '/company_analysis.json'
        with open(analysis_file, 'w', encoding='utf-8') as f:
            json.dump(analysis, f, indent=2, ensure_ascii=False)
        
        print(f"\n✓ Company analysis saved to: {analysis_file}")
        print(f"✓ Found {len(analysis['urls'])} URLs")
        print(f"✓ Found {len(analysis['emails'])} email addresses")
        print(f"✓ Found {len(analysis['metrics'])} metrics")
    
    print(f"\n✓ Extraction complete. All data saved to: {output_dir}")

if __name__ == '__main__':
    main()
```

**Execute the extraction:**

```bash
python3 /tmp/company-product-context/extract_pdfs.py
```

**Outputs:**
- `/tmp/extracted_data/[filename]_extracted.json` - Structured data per PDF
- `/tmp/extracted_data/company_analysis.json` - Aggregated analysis

## **Step 3: Structure extracted data**

Organize the extracted information into a structured format.

**Review extracted data:**

```bash
# List all extracted files
ls -la /tmp/extracted_data/

# Review company analysis
cat /tmp/extracted_data/company_analysis.json | jq '.'

# Review individual extractions
for file in /tmp/extracted_data/*_extracted.json; do
    echo "=== $(basename $file) ==="
    cat "$file" | jq '.metadata, .sections | keys'
done
```

**Manually review and note:**
- Company name and full legal name
- Core products and services
- Business model and revenue streams
- Target customers and market segments
- Key differentiators
- Technology stack or platform details
- Financial highlights
- Strategic initiatives

## **Step 4: Conduct web research and validation**

**Note:** This step requires web search capabilities. Based on extracted information:

**Research focus areas:**
1. **Company verification**: Confirm company details, recent news, press releases
2. **Product information**: Latest product updates, feature sets, pricing
3. **Market position**: Industry reports, analyst coverage, competitive landscape
4. **Customer base**: Case studies, testimonials, major clients
5. **Technology**: Tech stack, integrations, API documentation
6. **Recent developments**: Funding rounds, partnerships, acquisitions

**Search queries to execute:**
- "[Company Name] official website"
- "[Company Name] products and services"
- "[Company Name] company overview"
- "[Company Name] industry analysis"
- "[Company Name] competitors"
- "[Company Name] case studies"
- "[Company Name] recent news"
- "[Company Name] technology stack"

**Document findings in:**
```bash
# Create research notes file
cat > /tmp/extracted_data/web_research.md << 'EOF'
# Web Research Findings

## Official Sources
- Website: [URL]
- LinkedIn: [URL]
- Documentation: [URL]

## Company Overview
[Key findings from official sources]

## Products & Services
[Detailed product information]

## Market Position
[Industry context and competitive landscape]

## Recent Developments
[News, funding, partnerships]

## Technology Details
[Technical architecture, integrations]

## Customer Information
[Target market, case studies, testimonials]

## Additional Insights
[Other relevant findings]
EOF
```

Edit this file with your research findings.

## **Step 5: Synthesize industry knowledge**

Apply industry expertise and context to enrich the company profile.

**Industry analysis framework:**

Create an industry context document:

```bash
cat > /tmp/extracted_data/industry_context.md << 'EOF'
# Industry Context Analysis

## Industry Overview
- Industry: [Name]
- Market size: [Data]
- Growth rate: [Data]
- Key trends: [List]

## Competitive Landscape
- Major players: [List]
- Market segments: [Description]
- Competitive dynamics: [Analysis]

## Industry Challenges
1. [Challenge 1]
2. [Challenge 2]
3. [Challenge 3]

## Innovation Trends
1. [Trend 1]
2. [Trend 2]
3. [Trend 3]

## Regulatory Environment
[Relevant regulations and compliance requirements]

## Future Outlook
[Industry predictions and trajectory]

## Company Position in Industry
- Market segment: [Position]
- Competitive advantages: [List]
- Challenges faced: [List]
- Opportunities: [List]
EOF
```

**Key considerations:**
- Industry-specific terminology and concepts
- Regulatory requirements and compliance standards
- Common business models in the industry
- Typical customer pain points
- Standard technology solutions
- Industry best practices

## **Step 6: Compile comprehensive product context**

Synthesize all gathered information into a structured product context document.

**Use the compilation script:**

```python
import json
import os
from datetime import datetime
from pathlib import Path

def load_json_files(directory):
    """Load all JSON files from directory."""
    data = {}
    json_files = Path(directory).glob('*.json')
    
    for file in json_files:
        with open(file, 'r', encoding='utf-8') as f:
            data[file.stem] = json.load(f)
    
    return data

def load_markdown_files(directory):
    """Load all markdown files from directory."""
    data = {}
    md_files = Path(directory).glob('*.md')
    
    for file in md_files:
        with open(file, 'r', encoding='utf-8') as f:
            data[file.stem] = f.read()
    
    return data

def compile_product_context(json_data, markdown_data):
    """Compile comprehensive product context."""
    
    context = {
        'metadata': {
            'generated_date': datetime.now().isoformat(),
            'sources': list(json_data.keys()) + list(markdown_data.keys())
        },
        'company_profile': {},
        'products_and_services': {},
        'business_model': {},
        'market_analysis': {},
        'technology_platform': {},
        'customer_information': {},
        'strategic_context': {},
        'key_insights': []
    }
    
    # Extract company profile
    if 'company_analysis' in json_data:
        analysis = json_data['company_analysis']
        context['company_profile'] = {
            'name': analysis.get('company_name', ''),
            'industry': analysis.get('industry', ''),
            'website': analysis['urls'][0] if analysis.get('urls') else '',
            'contact': analysis['emails'][0] if analysis.get('emails') else '',
            'key_metrics': analysis.get('metrics', [])
        }
    
    # Compile product information
    products = []
    for key, data in json_data.items():
        if '_extracted' in key and 'sections' in data:
            sections = data['sections']
            if 'products_services' in sections:
                products.append(sections['products_services'])
    
    context['products_and_services'] = {
        'description': '\n\n'.join(products) if products else '',
        'categories': []
    }
    
    # Add web research
    if 'web_research' in markdown_data:
        context['web_research'] = markdown_data['web_research']
    
    # Add industry context
    if 'industry_context' in markdown_data:
        context['industry_analysis'] = markdown_data['industry_context']
    
    return context

def generate_narrative_report(context):
    """Generate narrative report from context data."""
    
    report = f"""# Company Product Context Report

**Generated:** {context['metadata']['generated_date']}

---

## Executive Summary

[This section provides a high-level overview of the company, its products, and market position.]

---

## Company Profile

### Overview
{context['company_profile'].get('name', 'Company Name')} operates in the {context['company_profile'].get('industry', 'industry')} sector.

**Key Details:**
- **Website:** {context['company_profile'].get('website', 'N/A')}
- **Industry:** {context['company_profile'].get('industry', 'N/A')}
- **Contact:** {context['company_profile'].get('contact', 'N/A')}

### Key Metrics
"""
    
    metrics = context['company_profile'].get('key_metrics', [])
    if metrics:
        for metric in metrics[:10]:  # Top 10 metrics
            report += f"- {metric}\n"
    else:
        report += "- [No metrics extracted]\n"
    
    report += """

---

## Products and Services

### Product Portfolio
"""
    
    products_desc = context['products_and_services'].get('description', '')
    if products_desc:
        report += products_desc
    else:
        report += "[Product information to be populated from extracted data]\n"
    
    report += """

### Service Offerings
[Service details from extracted information]

---

## Business Model

### Revenue Streams
[Revenue model and monetization strategy]

### Value Proposition
[Core value delivered to customers]

### Key Partnerships
[Strategic partnerships and ecosystem]

---

## Market Analysis

### Target Market
[Primary customer segments and market focus]

### Competitive Landscape
[Key competitors and market positioning]

### Market Opportunity
[Market size, growth potential, and trends]

---

## Technology Platform

### Technical Architecture
[Technology stack and infrastructure]

### Integration Capabilities
[APIs, integrations, and interoperability]

### Innovation Focus
[R&D initiatives and technological advantages]

---

## Customer Information

### Customer Profile
[Ideal customer profile and segments]

### Use Cases
[Common use cases and applications]

### Case Studies
[Notable customer implementations]

---

## Strategic Context

### Vision and Mission
[Company vision and mission statements]

### Strategic Priorities
[Current strategic initiatives and focus areas]

### Growth Strategy
[Expansion plans and growth initiatives]

---

## Industry Context

"""
    
    if 'industry_analysis' in context:
        report += context['industry_analysis']
    else:
        report += "[Industry analysis to be added]\n"
    
    report += """

---

## Web Research Findings

"""
    
    if 'web_research' in context:
        report += context['web_research']
    else:
        report += "[Web research findings to be added]\n"
    
    report += """

---

## Key Insights and Recommendations

### Strengths
1. [Key strength 1]
2. [Key strength 2]
3. [Key strength 3]

### Opportunities
1. [Opportunity 1]
2. [Opportunity 2]
3. [Opportunity 3]

### Challenges
1. [Challenge 1]
2. [Challenge 2]
3. [Challenge 3]

### Recommendations
1. [Recommendation 1]
2. [Recommendation 2]
3. [Recommendation 3]

---

## Appendix

### Sources
"""
    
    for source in context['metadata']['sources']:
        report += f"- {source}\n"
    
    report += """

### Methodology
This report was compiled using:
1. PDF document extraction and analysis
2. Web research and validation
3. Industry knowledge synthesis
4. Structured data compilation

---

*Report generated by Company Product Context Compiler*
"""
    
    return report

def main():
    data_dir = '/tmp/extracted_data'
    
    # Load all data
    print("Loading extracted data...")
    json_data = load_json_files(data_dir)
    markdown_data = load_markdown_files(data_dir)
    
    print(f"✓ Loaded {len(json_data)} JSON files")
    print(f"✓ Loaded {len(markdown_data)} Markdown files")
    
    # Compile context
    print("\nCompiling product context...")
    context = compile_product_context(json_data, markdown_data)
    
    # Save structured context
    context_file = '/tmp/product_context.json'
    with open(context_file, 'w', encoding='utf-8') as f:
        json.dump(context, f, indent=2, ensure_ascii=False)
    
    print(f"✓ Structured context saved to: {context_file}")
    
    # Generate narrative report
    print("\nGenerating narrative report...")
    report = generate_narrative_report(context)
    
    report_file = '/tmp/product_context_report.md'
    with open(report_file, 'w', encoding='utf-8') as f:
        f.write(report)
    
    print(f"✓ Narrative report saved to: {report_file}")
    
    print("\n✓ Product context compilation complete!")

if __name__ == '__main__':
    main()
```

**Execute compilation:**

```bash
python3 /tmp/company-product-context/compile_context.py
```

**Outputs:**
- `/tmp/product_context.json` - Structured data format
- `/tmp/product_context_report.md` - Narrative report

## **Step 7: Generate final report**

Review and enhance the generated report with manual insights and analysis.

**Review the report:**

```bash
cat /tmp/product_context_report.md
```

**Enhancement steps:**

1. **Fill in placeholders**: Replace bracketed placeholders with actual information
2. **Add analysis**: Include your expert analysis and insights
3. **Verify accuracy**: Cross-reference with source materials
4. **Add context**: Include industry-specific context and implications
5. **Enhance narrative**: Improve flow and readability
6. **Add visualizations**: Consider adding diagrams or charts (describe them textually)

**Create enhanced version:**

Edit `/tmp/product_context_report.md` to add:
- Executive summary with key takeaways
- Deeper analysis of competitive positioning
- Strategic recommendations
- Risk assessment
- Opportunity identification
- Implementation considerations

## **Step 8: Export deliverables**

Package and export all deliverables for the user.

**Export script:**

```bash
#!/bin/bash

OUTPUT_DIR=${OUTPUT_DIR:-/tmp/output}
EXPORT_DIR="/tmp/company_context_deliverables"

# Create export directory
mkdir -p "$EXPORT_DIR"

# Copy main deliverables
echo "Packaging deliverables..."

cp /tmp/product_context_report.md "$EXPORT_DIR/"
cp /tmp/product_context.json "$EXPORT_DIR/"

# Copy extracted data
mkdir -p "$EXPORT_DIR/raw_data"
cp -r /tmp/extracted_data/* "$EXPORT_DIR/raw_data/"

# Create summary document
cat > "$EXPORT_DIR/README.md" << 'EOF'
# Company Product Context Deliverables

## Contents

### Main Reports
1. **product_context_report.md** - Comprehensive narrative report
2. **product_context.json** - Structured data format

### Raw Data
- **raw_data/** - All extracted and intermediate data
  - PDF extractions (JSON format)
  - Company analysis
  - Web research notes
  - Industry context analysis

## How to Use

### The Narrative Report
Open `product_context_report.md` in any markdown viewer or text editor.
This is your primary deliverable with comprehensive analysis.

### The Structured Data
`product_context.json` contains machine-readable structured data
that can be imported into other systems or databases.

### Raw Data
The raw_data folder contains all intermediate processing files
for reference and verification purposes.

## Next Steps

1. Review the product context report
2. Validate information against your knowledge
3. Share with relevant stakeholders
4. Use as basis for strategic planning
5. Update as company evolves

---

Generated by Company Product Context Compiler
EOF

# Create archive
cd /tmp
tar -czf company_context_deliverables.tar.gz company_context_deliverables/

# Copy to output
cp -r "$EXPORT_DIR" "$OUTPUT_DIR/"
cp company_context_deliverables.tar.gz "$OUTPUT_DIR/"

echo "✓ Deliverables packaged"
echo "✓ Location: $EXPORT_DIR"
echo "✓ Archive: company_context_deliverables.tar.gz"
echo ""
echo "Files ready for download:"
ls -lh "$EXPORT_DIR"
```

**Execute export:**

```bash
bash /tmp/company-product-context/export_deliverables.sh
```

**Final deliverables:**
- 📄 Product Context Report (Markdown)
- 📊 Structured Data (JSON)
- 📁 Raw Extracted Data
- 📦 Complete Archive (tar.gz)
- 📖 README with usage instructions

---

## Usage Examples

### Example 1: Technology Company
**Inputs:** Annual report PDF, product documentation PDF
**Focus:** SaaS products, B2B market positioning
**Output:** Comprehensive context with tech stack analysis

### Example 2: Manufacturing Company
**Inputs:** Company brochure PDF, investor presentation
**Focus:** Product lines, supply chain, market segments
**Output:** Detailed product portfolio and market analysis

### Example 3: Consulting Firm
**Inputs:** Service offerings PDF, case studies PDF
**Focus:** Service capabilities, client types, differentiators
**Output:** Service context with competitive positioning

---

## Tips for Best Results

1. **Provide multiple PDFs**: More sources = richer context
2. **Include diverse documents**: Annual reports, product sheets, presentations, case studies
3. **Specify focus areas**: Guide the analysis to your needs
4. **Review and enhance**: Generated report is a starting point for your expert analysis
5. **Update web research**: Manually add recent information not in PDFs
6. **Validate metrics**: Cross-check extracted numbers for accuracy
7. **Add industry context**: Leverage your domain expertise
8. **Customize sections**: Tailor the report structure to your needs

---

## Troubleshooting

### PDF extraction issues
- Ensure PDFs are text-based (not scanned images)
- For image-based PDFs, OCR preprocessing may be needed
- Large PDFs may take longer to process

### Missing information
- Not all PDFs contain all sections
- Use web research to fill gaps
- Add manual notes to placeholder sections

### Data accuracy
- Always verify extracted metrics
- Cross-reference multiple sources
- Use web research for validation

---

## Customization Options

### Modify extraction patterns
Edit `extract_pdfs.py` to customize:
- Section identification keywords
- Metric extraction patterns
- Data structure organization

### Customize report template
Edit `compile_context.py` to modify:
- Report sections and structure
- Analysis framework
- Output formatting

### Add industry-specific sections
Extend the report template with:
- Compliance and regulatory analysis
- Technology architecture deep-dive
- Financial modeling
- Risk assessment frameworks

---

## Integration Possibilities

This skill can be integrated with:
- CRM systems (import structured company data)
- Sales enablement platforms
- Competitive intelligence databases
- Market research tools
- Strategic planning frameworks

Export the JSON format for easy integration with other systems.
