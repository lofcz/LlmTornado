# Quick Start Guide - Company Product Context

Get started with the Company Product Context Compiler in 5 minutes!

## 🚀 Fast Track

```bash
# 1. Place your company PDF files in the input directory
# 2. Run these three commands:

python3 extract_pdfs.py
python3 compile_context.py
bash export_deliverables.sh

# 3. Check the output directory for your deliverables!
```

## 📋 Step-by-Step

### Step 1: Prepare Your Files (2 minutes)

Gather your company documents:
- ✓ Annual reports
- ✓ Product brochures
- ✓ Investor presentations
- ✓ Company overviews
- ✓ Technical documentation

Place all PDF files in one directory.

### Step 2: Extract Information (2-5 minutes)

```bash
# Set your input directory (optional - defaults to /tmp)
export INPUT_DIR=/path/to/your/pdfs

# Run extraction
python3 extract_pdfs.py
```

You'll see:
```
======================================================================
PDF EXTRACTION FOR COMPANY PRODUCT CONTEXT
======================================================================

✓ Found 3 PDF file(s)

[1/3] Processing: annual_report.pdf
----------------------------------------------------------------------
  ✓ Extracted 45 pages
  ✓ Found 6 content sections
  ✓ Total characters: 125,430
  ✓ Saved to: annual_report_extracted.json
```

### Step 3: Compile Context (1 minute)

```bash
python3 compile_context.py
```

You'll see:
```
======================================================================
COMPILING COMPANY PRODUCT CONTEXT
======================================================================

📂 Loading extracted data...
  ✓ Loaded 4 JSON files
  ✓ Loaded 0 Markdown files

🔄 Compiling product context...
  ✓ Structured context saved to: /tmp/product_context.json

📝 Generating narrative report...
  ✓ Narrative report saved to: /tmp/product_context_report.md

✓ Company: Example Corp
✓ Industry: Technology
✓ Sources processed: 3
```

### Step 4: Export Deliverables (30 seconds)

```bash
# Set your output directory (optional)
export OUTPUT_DIR=/path/to/output

# Export everything
bash export_deliverables.sh
```

You'll get:
```
======================================================================
EXPORTING COMPANY PRODUCT CONTEXT DELIVERABLES
======================================================================

📁 Creating export directory...
📄 Packaging main reports...
  ✓ Product Context Report (Markdown)
  ✓ Structured Context Data (JSON)

📊 Packaging raw data...
  ✓ Extracted data files

📋 Creating template files...
  ✓ Web research template
  ✓ Industry context template

✅ Ready for download and use!
```

### Step 5: Review Your Results (2-10 minutes)

Open the main report:
```bash
# View in terminal
cat /tmp/company_context_deliverables/reports/product_context_report.md

# Or open in your favorite Markdown viewer
code /tmp/company_context_deliverables/reports/product_context_report.md
```

## 🎯 What You Get

After running these steps, you'll have:

1. **📄 Product Context Report**
   - Comprehensive narrative report
   - All company information organized
   - Ready to share with stakeholders

2. **📊 Structured Data (JSON)**
   - Machine-readable format
   - Ready for system integration
   - Contains all extracted information

3. **📁 Raw Data Files**
   - Individual PDF extractions
   - Company analysis
   - All intermediate processing

4. **📋 Research Templates**
   - Web research guide
   - Industry analysis template
   - Ready to fill in additional information

5. **📖 Complete Documentation**
   - README with instructions
   - Summary of contents
   - Usage guidelines

## 🔧 Common Issues

### Issue: "No PDF files found"
**Solution**: Make sure PDFs are in the INPUT_DIR or /tmp directory

### Issue: "PyPDF2 not found"
**Solution**: Install it: `pip install PyPDF2`

### Issue: "Permission denied"
**Solution**: Make scripts executable: `chmod +x *.py *.sh`

## ✨ Pro Tips

### Get Better Results

1. **Use Multiple PDFs**: More sources = richer context
2. **Recent Documents**: Use up-to-date materials
3. **Diverse Types**: Mix reports, brochures, presentations
4. **Text-Based PDFs**: Avoid scanned images

### Enhance Your Report

After initial generation:

1. **Fill in the templates** in `/templates/`
2. **Add web research** using the provided guide
3. **Include industry context** with domain expertise
4. **Re-run compilation** to regenerate with new data

```bash
# After adding research files to /tmp/extracted_data/
python3 compile_context.py
bash export_deliverables.sh
```

## 📚 Next Steps

### Immediate Actions
1. ✓ Review the generated report
2. ✓ Identify missing information
3. ✓ Use templates to add research

### Short Term (This Week)
4. ✓ Fill in web research template
5. ✓ Add industry context
6. ✓ Regenerate report with complete data
7. ✓ Share with stakeholders

### Long Term (Ongoing)
8. ✓ Update quarterly with new information
9. ✓ Track company changes
10. ✓ Maintain competitive intelligence
11. ✓ Use for strategic planning

## 🎓 Learn More

- **Full Documentation**: See `SKILL.md` for complete workflow
- **Detailed README**: Check `README.md` for features and customization
- **Templates**: Review `/templates/` for research guides

## 💡 Example Use Cases

### Sales Team
```bash
# Generate context before sales call
python3 extract_pdfs.py  # Using prospect's materials
python3 compile_context.py
# Review report for talking points
```

### Investment Analysis
```bash
# Analyze acquisition target
# Add all available documents to input
python3 extract_pdfs.py
python3 compile_context.py
# Review for due diligence
```

### Competitive Research
```bash
# Build competitor profile
# Gather competitor materials
python3 extract_pdfs.py
python3 compile_context.py
# Compare with internal data
```

## ⚡ Performance Notes

- **Small company** (1-2 PDFs, <100 pages): ~2-3 minutes total
- **Medium company** (3-5 PDFs, 100-300 pages): ~5-8 minutes total
- **Large company** (6+ PDFs, 300+ pages): ~10-15 minutes total

Processing time depends on:
- Number and size of PDF files
- Text extraction complexity
- System performance

## 🎉 You're Ready!

You now have everything you need to:
- ✅ Extract company information from PDFs
- ✅ Compile comprehensive product context
- ✅ Generate professional reports
- ✅ Share insights with your team

Happy analyzing! 🚀

---

**Questions?** Check the full SKILL.md documentation or README.md for detailed information.
