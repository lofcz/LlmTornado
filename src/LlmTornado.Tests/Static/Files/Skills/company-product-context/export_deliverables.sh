#!/bin/bash
#
# Export Deliverables Script
# Packages all company product context deliverables for download
#

set -e

OUTPUT_DIR=${OUTPUT_DIR:-/tmp/output}
EXPORT_DIR="/tmp/company_context_deliverables"

echo "======================================================================"
echo "EXPORTING COMPANY PRODUCT CONTEXT DELIVERABLES"
echo "======================================================================"

# Create export directory
echo ""
echo "📁 Creating export directory..."
mkdir -p "$EXPORT_DIR"
mkdir -p "$EXPORT_DIR/raw_data"
mkdir -p "$EXPORT_DIR/reports"
mkdir -p "$EXPORT_DIR/templates"

# Copy main deliverables
echo "📄 Packaging main reports..."

if [ -f /tmp/product_context_report.md ]; then
    cp /tmp/product_context_report.md "$EXPORT_DIR/reports/"
    echo "  ✓ Product Context Report (Markdown)"
else
    echo "  ⚠ Product context report not found"
fi

if [ -f /tmp/product_context.json ]; then
    cp /tmp/product_context.json "$EXPORT_DIR/reports/"
    echo "  ✓ Structured Context Data (JSON)"
else
    echo "  ⚠ Structured context not found"
fi

# Copy extracted data
echo ""
echo "📊 Packaging raw data..."

if [ -d /tmp/extracted_data ]; then
    cp -r /tmp/extracted_data/* "$EXPORT_DIR/raw_data/" 2>/dev/null || echo "  ⚠ No extracted data found"
    echo "  ✓ Extracted data files"
else
    echo "  ⚠ Extracted data directory not found"
fi

# Create template files for manual input
echo ""
echo "📋 Creating template files..."

# Web research template
cat > "$EXPORT_DIR/templates/web_research_template.md" << 'EOF'
# Web Research Findings Template

Use this template to document your web research findings.

## Official Sources
- **Company Website**: [URL]
- **LinkedIn**: [URL]
- **Twitter/X**: [URL]
- **Documentation**: [URL]
- **Blog**: [URL]

## Company Overview

### Recent News
- [Date] - [News item 1]
- [Date] - [News item 2]
- [Date] - [News item 3]

### Press Releases
- [Date] - [Press release 1]
- [Date] - [Press release 2]

## Products & Services

### Product Details
**Product/Service 1:**
- Description:
- Key features:
- Pricing:
- Target market:

**Product/Service 2:**
- Description:
- Key features:
- Pricing:
- Target market:

### Latest Updates
- [Feature/update 1]
- [Feature/update 2]

## Market Position

### Industry Reports
- **Gartner**: [Findings]
- **Forrester**: [Findings]
- **IDC**: [Findings]

### Market Share
- [Market segment]: [Percentage/position]

### Competitive Analysis
**Direct Competitors:**
1. [Competitor 1] - [Key differences]
2. [Competitor 2] - [Key differences]
3. [Competitor 3] - [Key differences]

## Recent Developments

### Funding & Investment
- **Last Round**: [Amount] in [Date]
- **Total Raised**: [Amount]
- **Valuation**: [Amount]
- **Key Investors**: [List]

### Partnerships
- [Date] - [Partnership 1]
- [Date] - [Partnership 2]

### Acquisitions
- [Date] - [Acquisition 1]
- [Date] - [Acquisition 2]

## Technology Details

### Tech Stack
- **Frontend**: [Technologies]
- **Backend**: [Technologies]
- **Infrastructure**: [Cloud provider, etc.]
- **Database**: [Technologies]

### Integrations
- [Integration 1]
- [Integration 2]
- [Integration 3]

### API Documentation
- **API Type**: [REST, GraphQL, etc.]
- **Documentation URL**: [URL]
- **Key Capabilities**: [List]

## Customer Information

### Notable Customers
1. [Customer 1] - [Industry] - [Use case]
2. [Customer 2] - [Industry] - [Use case]
3. [Customer 3] - [Industry] - [Use case]

### Customer Reviews
**G2:**
- Rating: [X.X/5]
- Number of reviews: [N]
- Key feedback: [Summary]

**Capterra:**
- Rating: [X.X/5]
- Number of reviews: [N]
- Key feedback: [Summary]

### Case Studies
1. **[Customer Name]**
   - Challenge: [Description]
   - Solution: [Description]
   - Results: [Metrics]

## Additional Insights

### Social Media Presence
- **LinkedIn Followers**: [Number]
- **Twitter Followers**: [Number]
- **Engagement**: [High/Medium/Low]

### Content Marketing
- Blog frequency: [Frequency]
- Content topics: [List]
- Thought leadership: [Assessment]

### Community & Events
- User conferences: [List]
- Community size: [Number]
- Event presence: [Assessment]

## Sources
- [Source 1]
- [Source 2]
- [Source 3]
EOF

# Industry context template
cat > "$EXPORT_DIR/templates/industry_context_template.md" << 'EOF'
# Industry Context Analysis Template

Use this template to document industry analysis and context.

## Industry Overview

### Industry Definition
**Industry Name**: [Name]
**Description**: [Brief description]

### Market Size & Growth
- **Global Market Size**: $[X] billion ([Year])
- **CAGR**: [X]% ([Year range])
- **Projected Size**: $[X] billion by [Year]

### Key Industry Segments
1. [Segment 1] - [% of market]
2. [Segment 2] - [% of market]
3. [Segment 3] - [% of market]

## Competitive Landscape

### Market Structure
- **Market Type**: [Consolidated/Fragmented/Emerging]
- **Number of Major Players**: [N]
- **Top 3 Market Leaders**: [List with market share]

### Major Players
**Leader 1:**
- Market share: [%]
- Key strengths:
- Key products:

**Leader 2:**
- Market share: [%]
- Key strengths:
- Key products:

**Leader 3:**
- Market share: [%]
- Key strengths:
- Key products:

### Competitive Dynamics
- **Barriers to Entry**: [High/Medium/Low] - [Explanation]
- **Switching Costs**: [High/Medium/Low] - [Explanation]
- **Threat of Substitutes**: [High/Medium/Low] - [Explanation]
- **Buyer Power**: [High/Medium/Low] - [Explanation]
- **Supplier Power**: [High/Medium/Low] - [Explanation]

## Industry Challenges

### Current Challenges
1. **[Challenge 1]**
   - Description: [Details]
   - Impact: [Assessment]
   
2. **[Challenge 2]**
   - Description: [Details]
   - Impact: [Assessment]

3. **[Challenge 3]**
   - Description: [Details]
   - Impact: [Assessment]

### Emerging Challenges
1. [Challenge 1]
2. [Challenge 2]
3. [Challenge 3]

## Innovation Trends

### Technology Trends
1. **[Trend 1]** - [Description and impact]
2. **[Trend 2]** - [Description and impact]
3. **[Trend 3]** - [Description and impact]

### Business Model Innovation
1. [Innovation 1]
2. [Innovation 2]
3. [Innovation 3]

### Customer Behavior Trends
1. [Trend 1]
2. [Trend 2]
3. [Trend 3]

## Regulatory Environment

### Key Regulations
1. **[Regulation 1]**
   - Description: [Details]
   - Impact on industry: [Assessment]

2. **[Regulation 2]**
   - Description: [Details]
   - Impact on industry: [Assessment]

### Compliance Requirements
- [Requirement 1]
- [Requirement 2]
- [Requirement 3]

### Upcoming Regulatory Changes
- [Date] - [Expected change 1]
- [Date] - [Expected change 2]

## Future Outlook

### 3-Year Outlook
- Market growth: [Projection]
- Key drivers: [List]
- Expected changes: [List]

### 5-Year Outlook
- Market evolution: [Description]
- Disruptive forces: [List]
- Opportunity areas: [List]

### Wild Cards
1. [Potential disruption 1]
2. [Potential disruption 2]
3. [Potential disruption 3]

## Company Position in Industry

### Current Position
- **Market Segment**: [Segment name]
- **Market Share**: [Percentage or description]
- **Ranking**: [Position] in [Category]

### Competitive Advantages
1. [Advantage 1] - [Explanation]
2. [Advantage 2] - [Explanation]
3. [Advantage 3] - [Explanation]

### Challenges Faced
1. [Challenge 1]
2. [Challenge 2]
3. [Challenge 3]

### Opportunities
1. **[Opportunity 1]**
   - Description: [Details]
   - Potential impact: [Assessment]

2. **[Opportunity 2]**
   - Description: [Details]
   - Potential impact: [Assessment]

3. **[Opportunity 3]**
   - Description: [Details]
   - Potential impact: [Assessment]

### Strategic Positioning
**Recommended Position**: [Description]
**Rationale**: [Explanation]

## Sources & References
- [Source 1]
- [Source 2]
- [Source 3]
EOF

echo "  ✓ Web research template"
echo "  ✓ Industry context template"

# Create README
echo ""
echo "📖 Creating documentation..."

cat > "$EXPORT_DIR/README.md" << 'EOF'
# Company Product Context Deliverables

This package contains all deliverables from the Company Product Context compilation process.

## 📁 Package Contents

### `/reports/` - Main Deliverables

1. **product_context_report.md**
   - Comprehensive narrative report
   - Formatted in Markdown for easy viewing
   - Contains all compiled information and analysis
   - **This is your primary deliverable**

2. **product_context.json**
   - Machine-readable structured data
   - JSON format for system integration
   - Contains all extracted and compiled information
   - Use for importing into databases or other systems

### `/raw_data/` - Source Data

Contains all intermediate processing files:
- **[filename]_extracted.json** - Individual PDF extractions
- **company_analysis.json** - Aggregated company analysis
- **web_research.md** - Web research findings (if added)
- **industry_context.md** - Industry analysis (if added)

### `/templates/` - Input Templates

Template files for manual data entry:
- **web_research_template.md** - Guide for web research
- **industry_context_template.md** - Guide for industry analysis

## 🚀 Quick Start

### 1. Review the Main Report
Open `reports/product_context_report.md` in any of these tools:
- Markdown viewer (Typora, MarkText, etc.)
- Code editor (VS Code, Sublime, etc.)
- GitHub/GitLab (upload to view formatted)
- Markdown to PDF converter

### 2. Complete Missing Information
The report may contain placeholder sections marked with:
- `[Bracketed text]` - Needs to be filled in
- `*Italicized bullets*` - Suggested content to add

### 3. Add Research Findings
If you haven't already:
1. Use the templates in `/templates/` as guides
2. Conduct web research using the template structure
3. Add industry analysis using domain expertise
4. Save completed files to `/raw_data/`
5. Re-run the compilation to regenerate the report

### 4. Validate and Enhance
- Cross-reference with official sources
- Verify extracted metrics
- Add expert analysis and insights
- Include competitive intelligence
- Update strategic recommendations

## 📊 Using the Structured Data

The `product_context.json` file can be:
- Imported into CRM systems
- Used for competitive intelligence databases
- Integrated with sales enablement platforms
- Parsed for custom analytics
- Used as input for AI/ML systems

### JSON Structure
```json
{
  "metadata": { ... },
  "company_profile": { ... },
  "products_and_services": { ... },
  "business_model": { ... },
  "market_analysis": { ... },
  "technology_platform": { ... },
  "customer_information": { ... },
  "strategic_context": { ... },
  "financial_information": { ... }
}
```

## 🔄 Updating the Context

To update the product context:

1. **Add new source documents**
   - Place new PDF files in the input directory
   - Re-run the extraction script

2. **Update web research**
   - Edit or create `web_research.md` in raw_data/
   - Follow the template structure

3. **Update industry analysis**
   - Edit or create `industry_context.md` in raw_data/
   - Include latest market data

4. **Recompile the report**
   - Run the compilation script
   - Review the updated report

## 💡 Best Practices

### For Accuracy
- ✓ Verify all extracted metrics with official sources
- ✓ Cross-reference information across multiple documents
- ✓ Update regularly as company evolves
- ✓ Include source citations for all claims

### For Completeness
- ✓ Fill in all [bracketed] placeholders
- ✓ Add industry-specific context
- ✓ Include competitive analysis
- ✓ Document strategic implications

### For Usability
- ✓ Use clear, concise language
- ✓ Structure information logically
- ✓ Include visual descriptions where helpful
- ✓ Provide actionable insights

## 🎯 Use Cases

### Sales Enablement
- Competitive positioning
- Product differentiators
- Customer use cases
- ROI justification

### Strategic Planning
- Market opportunity assessment
- Competitive landscape analysis
- Strategic recommendations
- Growth opportunities

### Investment Analysis
- Company due diligence
- Market position evaluation
- Technology assessment
- Risk analysis

### Marketing
- Messaging and positioning
- Content development
- Competitive intelligence
- Thought leadership

## 🔧 Troubleshooting

### Missing Information
**Problem**: Report has many empty sections
**Solution**: 
- Ensure source PDFs contain relevant information
- Add web research manually
- Use templates to structure additional research

### Extraction Errors
**Problem**: Information not extracted correctly
**Solution**:
- Check PDF text quality (not scanned images)
- Verify PDF files are not password-protected
- Review raw extraction files for issues

### Incomplete Analysis
**Problem**: Analysis lacks depth
**Solution**:
- Add domain expertise manually
- Include industry context from templates
- Enhance with competitive intelligence
- Add strategic recommendations based on findings

## 📚 Additional Resources

### Recommended Research Sources
- Company official website and blog
- LinkedIn company and employee profiles
- Industry analyst reports (Gartner, Forrester, IDC)
- Market research databases
- Customer review sites (G2, Capterra, TrustRadius)
- News and media coverage
- SEC filings (for public companies)
- Patent databases

### Analysis Frameworks
- Porter's Five Forces (competitive analysis)
- SWOT Analysis (strategic assessment)
- Business Model Canvas (business model)
- Value Chain Analysis (operations)
- TAM/SAM/SOM (market sizing)

## 📞 Next Steps

1. **Review** - Read through the complete report
2. **Validate** - Verify information accuracy
3. **Enhance** - Add missing analysis and insights
4. **Share** - Distribute to stakeholders
5. **Act** - Implement strategic recommendations
6. **Update** - Refresh regularly as company evolves

## 📄 Document Versions

Keep track of updates:
- v1.0 - Initial extraction and compilation
- v1.1 - Added web research
- v1.2 - Added industry context
- v1.3 - Enhanced with expert analysis

## 🙏 Feedback

This deliverable package was generated by the Company Product Context Compiler skill.
The quality of output depends on:
- Quality and comprehensiveness of source documents
- Addition of web research and industry context
- Domain expertise applied to analysis
- Regular updates with latest information

---

**Generated**: [Timestamp]
**Compiler Version**: 1.0
**Source Documents**: [Count]

For questions or issues, review the SKILL.md documentation or consult the skill creator.
EOF

echo "  ✓ README created"

# Create summary file
echo ""
echo "📈 Generating summary..."

cat > "$EXPORT_DIR/SUMMARY.txt" << EOF
========================================
COMPANY PRODUCT CONTEXT - SUMMARY
========================================

Generated: $(date '+%Y-%m-%d %H:%M:%S')

DELIVERABLES
------------
✓ Product Context Report (Markdown)
✓ Structured Data (JSON)
✓ Raw Extracted Data (JSON files)
✓ Input Templates (Markdown)
✓ README Documentation

FILES INCLUDED
--------------
EOF

# List all files
find "$EXPORT_DIR" -type f | sort | while read file; do
    rel_path=${file#$EXPORT_DIR/}
    file_size=$(du -h "$file" | cut -f1)
    echo "  $file_size  $rel_path" >> "$EXPORT_DIR/SUMMARY.txt"
done

cat >> "$EXPORT_DIR/SUMMARY.txt" << EOF

NEXT STEPS
----------
1. Open reports/product_context_report.md
2. Review all sections for completeness
3. Fill in [bracketed] placeholders
4. Add web research using templates
5. Include industry context
6. Validate all information
7. Share with stakeholders

USAGE TIPS
----------
- View .md files in a Markdown viewer
- Import .json files into other systems
- Use templates for structured research
- Keep raw data for reference
- Update regularly as company evolves

========================================
EOF

echo "  ✓ Summary created"

# Create archive
echo ""
echo "📦 Creating archive..."

cd /tmp
if tar -czf company_context_deliverables.tar.gz company_context_deliverables/ 2>/dev/null; then
    echo "  ✓ Archive created: company_context_deliverables.tar.gz"
    
    # Show archive size
    archive_size=$(du -h company_context_deliverables.tar.gz | cut -f1)
    echo "  ✓ Archive size: $archive_size"
else
    echo "  ⚠ Archive creation failed (non-critical)"
fi

# Copy to output directory
echo ""
echo "📤 Copying to output directory..."

cp -r "$EXPORT_DIR" "$OUTPUT_DIR/" 2>/dev/null && echo "  ✓ Deliverables folder copied" || echo "  ⚠ Could not copy to OUTPUT_DIR"
[ -f /tmp/company_context_deliverables.tar.gz ] && cp /tmp/company_context_deliverables.tar.gz "$OUTPUT_DIR/" 2>/dev/null && echo "  ✓ Archive copied" || true

# Final summary
echo ""
echo "======================================================================"
echo "EXPORT COMPLETE"
echo "======================================================================"
echo ""
echo "📍 Location: $EXPORT_DIR"
echo ""
echo "📦 Package Contents:"
echo "  • Product Context Report (Markdown)"
echo "  • Structured Data (JSON)"
echo "  • Raw Extracted Data"
echo "  • Input Templates"
echo "  • Documentation (README + SUMMARY)"
echo ""
echo "📊 File Statistics:"
file_count=$(find "$EXPORT_DIR" -type f | wc -l)
total_size=$(du -sh "$EXPORT_DIR" | cut -f1)
echo "  • Total files: $file_count"
echo "  • Total size: $total_size"
echo ""
echo "✅ Ready for download and use!"
echo ""
