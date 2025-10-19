#!/usr/bin/env python3
"""
PDF Extraction Script for Company Product Context
Extracts and structures information from company PDF documents.
"""

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
                    'creator': pdf_reader.metadata.get('/Creator', ''),
                    'producer': pdf_reader.metadata.get('/Producer', ''),
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
                    print(f"  ⚠ Error extracting page {page_num}: {e}")
                    
    except Exception as e:
        print(f"  ✗ Error reading PDF {pdf_path}: {e}")
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
        'company_overview': [
            'about us', 'company overview', 'who we are', 'introduction', 
            'history', 'our company', 'company profile', 'background',
            'founded', 'established', 'headquarters'
        ],
        'products_services': [
            'products', 'services', 'solutions', 'offerings', 'portfolio',
            'what we do', 'our products', 'our services', 'product line',
            'capabilities', 'features'
        ],
        'business_model': [
            'business model', 'revenue model', 'how we work', 'operations',
            'business operations', 'operating model', 'value chain'
        ],
        'market_position': [
            'market', 'industry', 'competitive', 'position', 'landscape',
            'market share', 'market leader', 'market opportunity',
            'addressable market', 'tam', 'market size'
        ],
        'financials': [
            'financial', 'revenue', 'earnings', 'profit', 'growth',
            'income', 'balance sheet', 'cash flow', 'ebitda',
            'quarterly results', 'annual results', 'financial performance'
        ],
        'technology': [
            'technology', 'platform', 'infrastructure', 'technical', 
            'innovation', 'architecture', 'tech stack', 'engineering',
            'software', 'hardware', 'systems', 'r&d', 'research'
        ],
        'customers': [
            'customers', 'clients', 'partners', 'case study', 'testimonial',
            'user', 'customer success', 'client stories', 'references',
            'implementations'
        ],
        'strategy': [
            'strategy', 'vision', 'mission', 'goals', 'objectives', 
            'roadmap', 'strategic', 'future', 'plans', 'priorities',
            'initiatives', 'direction'
        ]
    }
    
    lines = text.split('\n')
    current_section = 'other'
    
    for line in lines:
        line_lower = line.lower().strip()
        
        # Check if line is a section header
        if line_lower and len(line_lower) < 150:  # Potential header
            for section, section_keywords in keywords.items():
                if any(keyword in line_lower for keyword in section_keywords):
                    if len(line_lower) < 100:  # Likely a header
                        current_section = section
                        break
        
        if line.strip():
            sections[current_section].append(line)
    
    return sections

def extract_entities(text):
    """Extract named entities and key information from text."""
    entities = {
        'companies': set(),
        'products': set(),
        'technologies': set(),
        'locations': set(),
        'people': set()
    }
    
    # Technology keywords
    tech_keywords = [
        'AI', 'ML', 'API', 'SaaS', 'PaaS', 'IaaS', 'cloud', 'AWS', 'Azure',
        'GCP', 'Kubernetes', 'Docker', 'blockchain', 'IoT', 'analytics',
        'platform', 'mobile', 'web', 'database', 'CRM', 'ERP'
    ]
    
    # Look for capitalized phrases (potential companies/products)
    cap_pattern = r'\b[A-Z][a-z]+(?:\s+[A-Z][a-z]+)*\b'
    potential_entities = re.findall(cap_pattern, text)
    
    for entity in potential_entities:
        if len(entity) > 3 and entity not in ['The', 'This', 'That', 'These', 'Those']:
            entities['companies'].add(entity)
    
    # Look for technology terms
    for tech in tech_keywords:
        if re.search(r'\b' + re.escape(tech) + r'\b', text, re.IGNORECASE):
            entities['technologies'].add(tech)
    
    return {k: list(v) for k, v in entities.items()}

def analyze_company_info(extracted_data):
    """Analyze extracted data for key company information."""
    analysis = {
        'company_name': '',
        'industry': '',
        'products': [],
        'technologies': [],
        'key_terms': [],
        'metrics': [],
        'urls': [],
        'emails': [],
        'entities': {
            'companies': [],
            'products': [],
            'technologies': []
        }
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
    metrics_patterns = [
        r'\$\s*\d+\.?\d*\s*(?:million|billion|trillion|M|B|T)',
        r'\d+\.?\d*\s*(?:million|billion|trillion)\s*(?:dollars|users|customers|employees)',
        r'\d+\.?\d*\s*(?:%|percent)',
        r'\d+\.?\d*[kKmMbBtT]\s*(?:users|customers|employees|revenue)',
    ]
    
    for pattern in metrics_patterns:
        analysis['metrics'].extend(re.findall(pattern, all_text, re.IGNORECASE))
    
    # Extract entities
    entities = extract_entities(all_text)
    analysis['entities'] = entities
    
    # Extract potential company name (from metadata or common patterns)
    for doc in extracted_data:
        if doc['metadata'].get('title'):
            analysis['company_name'] = doc['metadata']['title']
            break
    
    return analysis

def main():
    input_dir = os.environ.get('INPUT_DIR', '/tmp')
    output_dir = '/tmp/extracted_data'
    os.makedirs(output_dir, exist_ok=True)
    
    print("=" * 70)
    print("PDF EXTRACTION FOR COMPANY PRODUCT CONTEXT")
    print("=" * 70)
    
    # Find all PDF files
    pdf_files = list(Path(input_dir).glob('*.pdf'))
    pdf_files.extend(Path(input_dir).glob('**/*.pdf'))
    pdf_files = list(set(pdf_files))  # Remove duplicates
    
    if not pdf_files:
        print("\n⚠ No PDF files found in input directory")
        print(f"  Searched in: {input_dir}")
        return
    
    print(f"\n✓ Found {len(pdf_files)} PDF file(s)\n")
    
    extracted_data = []
    
    for idx, pdf_file in enumerate(pdf_files, 1):
        print(f"[{idx}/{len(pdf_files)}] Processing: {pdf_file.name}")
        print("-" * 70)
        
        data = extract_pdf_content(str(pdf_file))
        
        if data:
            extracted_data.append(data)
            
            # Extract sections from content
            all_text = '\n'.join([page['text'] for page in data['content']])
            sections = extract_key_sections(all_text)
            
            # Count non-empty sections
            non_empty_sections = [k for k, v in sections.items() if v]
            
            # Save individual file data
            output_file = output_dir + f"/{pdf_file.stem}_extracted.json"
            with open(output_file, 'w', encoding='utf-8') as f:
                json.dump({
                    'metadata': data['metadata'],
                    'sections': {k: '\n'.join(v) for k, v in sections.items() if v},
                    'full_text': all_text[:1000] + '...' if len(all_text) > 1000 else all_text,
                    'stats': {
                        'total_pages': len(data['content']),
                        'total_chars': len(all_text),
                        'sections_found': non_empty_sections
                    }
                }, f, indent=2, ensure_ascii=False)
            
            print(f"  ✓ Extracted {len(data['content'])} pages")
            print(f"  ✓ Found {len(non_empty_sections)} content sections")
            print(f"  ✓ Total characters: {len(all_text):,}")
            print(f"  ✓ Saved to: {os.path.basename(output_file)}")
        else:
            print(f"  ✗ Failed to extract data")
        
        print()
    
    # Analyze all extracted data
    if extracted_data:
        print("=" * 70)
        print("ANALYZING EXTRACTED DATA")
        print("=" * 70)
        
        analysis = analyze_company_info(extracted_data)
        
        analysis_file = output_dir + '/company_analysis.json'
        with open(analysis_file, 'w', encoding='utf-8') as f:
            json.dump(analysis, f, indent=2, ensure_ascii=False)
        
        print(f"\n✓ Company analysis completed")
        print(f"  • URLs found: {len(analysis['urls'])}")
        print(f"  • Email addresses: {len(analysis['emails'])}")
        print(f"  • Metrics extracted: {len(analysis['metrics'])}")
        print(f"  • Technologies mentioned: {len(analysis['entities']['technologies'])}")
        print(f"  • Saved to: company_analysis.json")
        
        # Display some key findings
        if analysis['urls']:
            print(f"\n  Key URLs:")
            for url in analysis['urls'][:3]:
                print(f"    - {url}")
        
        if analysis['metrics']:
            print(f"\n  Sample metrics:")
            for metric in analysis['metrics'][:5]:
                print(f"    - {metric}")
    
    print("\n" + "=" * 70)
    print("EXTRACTION COMPLETE")
    print("=" * 70)
    print(f"✓ Processed {len(extracted_data)} PDF file(s)")
    print(f"✓ Output directory: {output_dir}")
    print(f"✓ Ready for context compilation\n")

if __name__ == '__main__':
    main()
