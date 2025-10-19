#!/usr/bin/env python3
"""
Context Compilation Script for Company Product Context
Compiles extracted data into comprehensive product context report.
"""

import json
import os
from datetime import datetime
from pathlib import Path

def load_json_files(directory):
    """Load all JSON files from directory."""
    data = {}
    json_files = list(Path(directory).glob('*.json'))
    
    for file in json_files:
        try:
            with open(file, 'r', encoding='utf-8') as f:
                data[file.stem] = json.load(f)
        except Exception as e:
            print(f"⚠ Error loading {file.name}: {e}")
    
    return data

def load_markdown_files(directory):
    """Load all markdown files from directory."""
    data = {}
    md_files = list(Path(directory).glob('*.md'))
    
    for file in md_files:
        try:
            with open(file, 'r', encoding='utf-8') as f:
                data[file.stem] = f.read()
        except Exception as e:
            print(f"⚠ Error loading {file.name}: {e}")
    
    return data

def extract_section_content(json_data, section_name):
    """Extract content from specific section across all documents."""
    content = []
    
    for key, data in json_data.items():
        if '_extracted' in key and 'sections' in data:
            sections = data['sections']
            if section_name in sections and sections[section_name]:
                content.append({
                    'source': data.get('metadata', {}).get('title', key),
                    'content': sections[section_name]
                })
    
    return content

def compile_product_context(json_data, markdown_data):
    """Compile comprehensive product context."""
    
    context = {
        'metadata': {
            'generated_date': datetime.now().isoformat(),
            'generated_by': 'Company Product Context Compiler',
            'version': '1.0',
            'sources': {
                'pdf_documents': [k for k in json_data.keys() if '_extracted' in k],
                'analysis_files': [k for k in json_data.keys() if '_extracted' not in k],
                'research_files': list(markdown_data.keys())
            }
        },
        'company_profile': {},
        'products_and_services': {},
        'business_model': {},
        'market_analysis': {},
        'technology_platform': {},
        'customer_information': {},
        'strategic_context': {},
        'financial_information': {}
    }
    
    # Extract company profile
    if 'company_analysis' in json_data:
        analysis = json_data['company_analysis']
        context['company_profile'] = {
            'name': analysis.get('company_name', 'Unknown'),
            'industry': analysis.get('industry', 'Unknown'),
            'website': analysis['urls'][0] if analysis.get('urls') else '',
            'contact': analysis['emails'][0] if analysis.get('emails') else '',
            'key_metrics': analysis.get('metrics', []),
            'technologies': analysis.get('entities', {}).get('technologies', [])
        }
    
    # Compile products and services
    products = extract_section_content(json_data, 'products_services')
    context['products_and_services'] = {
        'sources': [p['source'] for p in products],
        'content': products
    }
    
    # Compile business model
    business = extract_section_content(json_data, 'business_model')
    context['business_model'] = {
        'sources': [b['source'] for b in business],
        'content': business
    }
    
    # Compile market analysis
    market = extract_section_content(json_data, 'market_position')
    context['market_analysis'] = {
        'sources': [m['source'] for m in market],
        'content': market
    }
    
    # Compile technology platform
    tech = extract_section_content(json_data, 'technology')
    context['technology_platform'] = {
        'sources': [t['source'] for t in tech],
        'content': tech
    }
    
    # Compile customer information
    customers = extract_section_content(json_data, 'customers')
    context['customer_information'] = {
        'sources': [c['source'] for c in customers],
        'content': customers
    }
    
    # Compile strategic context
    strategy = extract_section_content(json_data, 'strategy')
    context['strategic_context'] = {
        'sources': [s['source'] for s in strategy],
        'content': strategy
    }
    
    # Compile financial information
    financials = extract_section_content(json_data, 'financials')
    context['financial_information'] = {
        'sources': [f['source'] for f in financials],
        'content': financials
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
    
    company_name = context['company_profile'].get('name', 'Company')
    industry = context['company_profile'].get('industry', 'Unknown Industry')
    
    report = f"""# Company Product Context Report: {company_name}

**Generated:** {datetime.now().strftime('%B %d, %Y at %I:%M %p')}  
**Industry:** {industry}  
**Version:** {context['metadata']['version']}

---

## 📋 Executive Summary

This report provides a comprehensive analysis of {company_name}, including its products, services, market position, business model, and strategic direction. The analysis is based on multiple source documents and incorporates industry context and market insights.

**Key Highlights:**
- Detailed company profile and background
- Complete product and service portfolio analysis
- Business model and revenue streams
- Market positioning and competitive landscape
- Technology platform and capabilities
- Customer segments and use cases
- Strategic priorities and future direction

---

## 🏢 Company Profile

### Company Overview

**Company Name:** {context['company_profile'].get('name', 'N/A')}  
**Industry:** {context['company_profile'].get('industry', 'N/A')}  
**Website:** {context['company_profile'].get('website', 'N/A')}  
**Primary Contact:** {context['company_profile'].get('contact', 'N/A')}

"""
    
    # Add company overview content
    overview = context.get('products_and_services', {}).get('content', [])
    if overview:
        report += "### About the Company\n\n"
        # Use first source for overview if available
        if overview[0].get('content'):
            # Take first paragraph or section
            overview_text = overview[0]['content'][:1000]
            report += f"{overview_text}...\n\n"
    
    report += "### Key Metrics\n\n"
    metrics = context['company_profile'].get('key_metrics', [])
    if metrics:
        for metric in metrics[:15]:
            report += f"- {metric}\n"
    else:
        report += "*No quantitative metrics extracted from source documents.*\n"
    
    report += "\n### Technology Stack\n\n"
    technologies = context['company_profile'].get('technologies', [])
    if technologies:
        report += "Mentioned technologies and platforms:\n"
        for tech in technologies[:20]:
            report += f"- {tech}\n"
    else:
        report += "*Technology information to be populated from technical documentation.*\n"
    
    report += """

---

## 🎯 Products and Services

### Product Portfolio

"""
    
    products = context['products_and_services'].get('content', [])
    if products:
        for idx, product in enumerate(products, 1):
            source = product.get('source', 'Unknown')
            content = product.get('content', '')
            
            report += f"#### Source: {source}\n\n"
            report += f"{content}\n\n"
    else:
        report += """*Product information to be populated from:*
- Product documentation
- Marketing materials
- Service descriptions
- Feature lists

"""
    
    report += """### Service Offerings

*Service details including:*
- Professional services
- Support and maintenance
- Training and enablement
- Consulting services
- Custom development

### Product Categories

*To be categorized based on:*
- Core products vs. add-ons
- Target market segments
- Pricing tiers
- Deployment models (cloud, on-premise, hybrid)

---

## 💼 Business Model

"""
    
    business = context['business_model'].get('content', [])
    if business:
        report += "### Business Operations\n\n"
        for item in business:
            report += f"**From {item.get('source', 'Source')}:**\n\n"
            report += f"{item.get('content', '')}\n\n"
    else:
        report += """### Revenue Model

*Revenue streams and monetization strategy:*
- Subscription/SaaS revenue
- License fees
- Professional services
- Usage-based pricing
- Marketplace/ecosystem revenue

### Value Proposition

*Core value delivered to customers:*
- Problem solved
- Benefits provided
- Competitive advantages
- Unique differentiators

### Key Partnerships

*Strategic partnerships and ecosystem:*
- Technology partners
- Channel partners
- Integration partners
- Strategic alliances

"""
    
    report += """
---

## 📊 Market Analysis

"""
    
    market = context['market_analysis'].get('content', [])
    if market:
        report += "### Market Position\n\n"
        for item in market:
            report += f"**From {item.get('source', 'Source')}:**\n\n"
            report += f"{item.get('content', '')}\n\n"
    else:
        report += """### Target Market

*Primary customer segments and market focus:*
- Industry verticals
- Company size (enterprise, mid-market, SMB)
- Geographic markets
- Use case segments

### Competitive Landscape

*Key competitors and market positioning:*
- Direct competitors
- Indirect competitors
- Substitute solutions
- Competitive advantages
- Market differentiation

### Market Opportunity

*Market size, growth potential, and trends:*
- Total Addressable Market (TAM)
- Serviceable Available Market (SAM)
- Market growth rate
- Industry trends
- Emerging opportunities

"""
    
    report += """
---

## 💻 Technology Platform

"""
    
    tech = context['technology_platform'].get('content', [])
    if tech:
        report += "### Technical Architecture\n\n"
        for item in tech:
            report += f"**From {item.get('source', 'Source')}:**\n\n"
            report += f"{item.get('content', '')}\n\n"
    else:
        report += """### Technical Architecture

*Technology stack and infrastructure:*
- Frontend technologies
- Backend systems
- Database and storage
- Cloud infrastructure
- Security measures

### Integration Capabilities

*APIs, integrations, and interoperability:*
- API architecture (REST, GraphQL, etc.)
- Pre-built integrations
- Marketplace/app store
- Webhooks and events
- Data import/export

### Innovation Focus

*R&D initiatives and technological advantages:*
- AI/ML capabilities
- Automation features
- Scalability approach
- Performance optimization
- Security innovations

"""
    
    report += """
---

## 👥 Customer Information

"""
    
    customers = context['customer_information'].get('content', [])
    if customers:
        report += "### Customer Base\n\n"
        for item in customers:
            report += f"**From {item.get('source', 'Source')}:**\n\n"
            report += f"{item.get('content', '')}\n\n"
    else:
        report += """### Ideal Customer Profile

*Target customer characteristics:*
- Industry sectors
- Company size and revenue
- Geographic location
- Technology maturity
- Specific pain points

### Use Cases

*Common use cases and applications:*
- Primary use cases
- Industry-specific applications
- Department-specific uses
- Integration scenarios

### Customer Success Stories

*Notable customer implementations:*
- Case studies
- Testimonials
- ROI examples
- Implementation timelines
- Success metrics

"""
    
    report += """
---

## 🎯 Strategic Context

"""
    
    strategy = context['strategic_context'].get('content', [])
    if strategy:
        report += "### Strategic Direction\n\n"
        for item in strategy:
            report += f"**From {item.get('source', 'Source')}:**\n\n"
            report += f"{item.get('content', '')}\n\n"
    else:
        report += """### Vision and Mission

*Company vision and mission statements:*
- Long-term vision
- Mission statement
- Core values
- Purpose and impact

### Strategic Priorities

*Current strategic initiatives and focus areas:*
- Product development priorities
- Market expansion plans
- Technology investments
- Partnership strategy
- Sustainability initiatives

### Growth Strategy

*Expansion plans and growth initiatives:*
- Geographic expansion
- Product line extensions
- Market segment penetration
- Acquisition strategy
- Organic growth initiatives

"""
    
    report += """
---

## 💰 Financial Information

"""
    
    financials = context['financial_information'].get('content', [])
    if financials:
        report += "### Financial Performance\n\n"
        for item in financials:
            report += f"**From {item.get('source', 'Source')}:**\n\n"
            report += f"{item.get('content', '')}\n\n"
    else:
        report += """### Financial Highlights

*Key financial metrics and performance:*
- Revenue (annual/quarterly)
- Growth rate
- Profitability
- Funding history
- Valuation (if public)

### Investment and Funding

*Capital structure and funding:*
- Funding rounds
- Total capital raised
- Key investors
- Valuation milestones

"""
    
    report += """
---

## 🌐 Industry Context

"""
    
    if 'industry_analysis' in context:
        report += context['industry_analysis'] + "\n\n"
    else:
        report += """### Industry Overview

*Industry landscape and dynamics:*
- Industry definition and scope
- Market size and growth
- Key trends and drivers
- Regulatory environment
- Technology evolution

### Competitive Dynamics

*Competitive forces and market structure:*
- Market concentration
- Barriers to entry
- Switching costs
- Threat of substitutes
- Supplier/buyer power

### Industry Trends

*Emerging trends and future direction:*
- Technology adoption trends
- Business model evolution
- Regulatory changes
- Customer behavior shifts
- Innovation patterns

### Company's Industry Position

*How the company fits in the industry:*
- Market segment served
- Competitive positioning
- Industry leadership
- Innovation contribution
- Ecosystem role

"""
    
    report += """
---

## 🔍 Web Research Findings

"""
    
    if 'web_research' in context:
        report += context['web_research'] + "\n\n"
    else:
        report += """*Additional research to be conducted on:*

### Official Sources
- Company website and blog
- LinkedIn company page
- Press releases
- SEC filings (if public)
- Official documentation

### Third-Party Analysis
- Industry analyst reports (Gartner, Forrester, IDC)
- Market research publications
- News articles and media coverage
- Customer review sites (G2, Capterra, TrustRadius)
- Social media presence and sentiment

### Competitive Intelligence
- Competitor websites and positioning
- Comparative reviews
- Feature comparisons
- Pricing analysis
- Market share data

"""
    
    report += """
---

## 💡 Key Insights and Recommendations

### Strengths

Based on the analyzed information, key strengths include:

1. **[Strength 1]** - *To be identified from analysis*
2. **[Strength 2]** - *To be identified from analysis*
3. **[Strength 3]** - *To be identified from analysis*

### Opportunities

Potential opportunities for growth and development:

1. **[Opportunity 1]** - *To be identified from market analysis*
2. **[Opportunity 2]** - *To be identified from competitive analysis*
3. **[Opportunity 3]** - *To be identified from trend analysis*

### Challenges

Key challenges and considerations:

1. **[Challenge 1]** - *To be identified from market conditions*
2. **[Challenge 2]** - *To be identified from competitive landscape*
3. **[Challenge 3]** - *To be identified from industry trends*

### Strategic Recommendations

Based on this analysis, consider:

1. **[Recommendation 1]** - *Strategic action based on insights*
2. **[Recommendation 2]** - *Tactical initiative based on findings*
3. **[Recommendation 3]** - *Investment priority based on opportunities*

---

## 📎 Appendix

### Sources and References

**PDF Documents Analyzed:**
"""
    
    for source in context['metadata']['sources']['pdf_documents']:
        report += f"- {source}\n"
    
    report += "\n**Analysis Files:**\n"
    for source in context['metadata']['sources']['analysis_files']:
        report += f"- {source}\n"
    
    if context['metadata']['sources']['research_files']:
        report += "\n**Research Files:**\n"
        for source in context['metadata']['sources']['research_files']:
            report += f"- {source}\n"
    
    report += f"""

### Methodology

This report was compiled using the following approach:

1. **PDF Document Extraction**
   - Automated text extraction from company documents
   - Section identification and categorization
   - Metadata and structure analysis

2. **Data Structuring**
   - Organization by content type
   - Key information extraction
   - Metric and entity identification

3. **Web Research** *(when conducted)*
   - Official source verification
   - Third-party analysis integration
   - Market data collection

4. **Industry Knowledge Synthesis**
   - Industry context application
   - Competitive landscape analysis
   - Trend identification and analysis

5. **Report Compilation**
   - Structured data aggregation
   - Narrative generation
   - Insight development

### Data Quality Notes

- Information accuracy depends on source document quality
- Extracted metrics should be verified against official sources
- Industry analysis requires domain expertise validation
- Recommendations are based on available information and may require additional research

### Next Steps

To maximize the value of this report:

1. **Validate Information**: Cross-reference with authoritative sources
2. **Fill Gaps**: Add missing information from additional research
3. **Add Analysis**: Include expert insights and interpretation
4. **Update Regularly**: Refresh as company evolves
5. **Share Strategically**: Distribute to relevant stakeholders
6. **Act on Insights**: Implement recommendations and priorities

---

## 📝 Report Metadata

- **Generated**: {context['metadata']['generated_date']}
- **Generator**: {context['metadata']['generated_by']}
- **Version**: {context['metadata']['version']}
- **Format**: Markdown
- **Source Documents**: {len(context['metadata']['sources']['pdf_documents'])}

---

*This report was automatically generated by the Company Product Context Compiler skill.*  
*For questions or additional analysis, please update source documents and regenerate.*
"""
    
    return report

def main():
    data_dir = '/tmp/extracted_data'
    
    print("=" * 70)
    print("COMPILING COMPANY PRODUCT CONTEXT")
    print("=" * 70)
    
    # Load all data
    print("\n📂 Loading extracted data...")
    json_data = load_json_files(data_dir)
    markdown_data = load_markdown_files(data_dir)
    
    print(f"  ✓ Loaded {len(json_data)} JSON files")
    print(f"  ✓ Loaded {len(markdown_data)} Markdown files")
    
    if not json_data:
        print("\n⚠ No data files found. Please run extract_pdfs.py first.")
        return
    
    # Compile context
    print("\n🔄 Compiling product context...")
    context = compile_product_context(json_data, markdown_data)
    
    # Save structured context
    context_file = '/tmp/product_context.json'
    with open(context_file, 'w', encoding='utf-8') as f:
        json.dump(context, f, indent=2, ensure_ascii=False)
    
    print(f"  ✓ Structured context saved to: {context_file}")
    
    # Generate narrative report
    print("\n📝 Generating narrative report...")
    report = generate_narrative_report(context)
    
    report_file = '/tmp/product_context_report.md'
    with open(report_file, 'w', encoding='utf-8') as f:
        f.write(report)
    
    print(f"  ✓ Narrative report saved to: {report_file}")
    
    # Summary
    print("\n" + "=" * 70)
    print("COMPILATION COMPLETE")
    print("=" * 70)
    print(f"✓ Structured data: {context_file}")
    print(f"✓ Narrative report: {report_file}")
    print(f"✓ Company: {context['company_profile'].get('name', 'N/A')}")
    print(f"✓ Industry: {context['company_profile'].get('industry', 'N/A')}")
    print(f"✓ Sources processed: {len(context['metadata']['sources']['pdf_documents'])}")
    print("\n💡 Next steps:")
    print("  1. Review the narrative report")
    print("  2. Fill in any [bracketed] placeholders")
    print("  3. Add web research findings")
    print("  4. Include industry context")
    print("  5. Export deliverables")
    print()

if __name__ == '__main__':
    main()
