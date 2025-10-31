#!/usr/bin/env python3
"""
Verify FSKB installation and dependencies.
Run this script after installation to ensure everything is working.
"""

import sys
from pathlib import Path

def check_python_version():
    """Check Python version."""
    print("Checking Python version...")
    version = sys.version_info
    if version.major < 3 or (version.major == 3 and version.minor < 9):
        print(f"❌ Python 3.9+ required, found {version.major}.{version.minor}")
        return False
    print(f"✅ Python {version.major}.{version.minor}.{version.micro}")
    return True


def check_dependencies():
    """Check if all required dependencies are installed."""
    print("\nChecking dependencies...")
    
    dependencies = [
        ("mcp", "MCP protocol"),
        ("sentence_transformers", "Local embeddings"),
        ("chromadb", "Vector storage"),
        ("watchdog", "File watching"),
        ("git", "Git operations (GitPython)"),
        ("pathspec", "Gitignore parsing"),
        ("PyQt6.QtWidgets", "GUI (optional)"),
        ("pydantic", "Configuration"),
        ("loguru", "Logging"),
        ("tenacity", "Retry logic"),
        ("psutil", "Resource monitoring"),
    ]
    
    missing = []
    for module, description in dependencies:
        try:
            __import__(module)
            print(f"✅ {description}")
        except ImportError:
            print(f"❌ {description} - {module} not found")
            missing.append(module)
    
    return len(missing) == 0, missing


def check_project_structure():
    """Check if project structure is correct."""
    print("\nChecking project structure...")
    
    required_paths = [
        "fskb/__init__.py",
        "fskb/config/settings.py",
        "fskb/indexing/indexing_engine.py",
        "fskb/storage/chroma_store.py",
        "fskb/search/query.py",
        "fskb/mcp_server/server.py",
        "fskb/gui/main_window.py",
        "fskb/utils/resource_manager.py",
        "main.py",
        "requirements.txt",
    ]
    
    missing = []
    for path in required_paths:
        if Path(path).exists():
            print(f"✅ {path}")
        else:
            print(f"❌ {path}")
            missing.append(path)
    
    return len(missing) == 0, missing


def check_data_directory():
    """Check if data directory can be created."""
    print("\nChecking data directory...")
    
    data_dir = Path.home() / ".fskb"
    
    try:
        data_dir.mkdir(parents=True, exist_ok=True)
        print(f"✅ Data directory: {data_dir}")
        
        # Check write permissions
        test_file = data_dir / ".test"
        test_file.write_text("test")
        test_file.unlink()
        print("✅ Write permissions OK")
        
        return True
    except Exception as e:
        print(f"❌ Error creating data directory: {e}")
        return False


def check_import():
    """Check if FSKB modules can be imported."""
    print("\nChecking FSKB imports...")
    
    modules = [
        "fskb.config",
        "fskb.indexing",
        "fskb.storage",
        "fskb.search",
        "fskb.utils",
    ]
    
    errors = []
    for module in modules:
        try:
            __import__(module)
            print(f"✅ {module}")
        except Exception as e:
            print(f"❌ {module}: {e}")
            errors.append((module, str(e)))
    
    return len(errors) == 0, errors


def main():
    """Run all verification checks."""
    print("=" * 60)
    print("FSKB Installation Verification")
    print("=" * 60)
    
    all_passed = True
    
    # Check Python version
    if not check_python_version():
        all_passed = False
    
    # Check dependencies
    deps_ok, missing_deps = check_dependencies()
    if not deps_ok:
        all_passed = False
        print(f"\n⚠️  Missing dependencies: {', '.join(missing_deps)}")
        print("Install with: pip install -r requirements.txt")
    
    # Check project structure
    structure_ok, missing_files = check_project_structure()
    if not structure_ok:
        all_passed = False
        print(f"\n⚠️  Missing files: {', '.join(missing_files)}")
    
    # Check data directory
    if not check_data_directory():
        all_passed = False
    
    # Check imports
    if structure_ok and deps_ok:
        import_ok, import_errors = check_import()
        if not import_ok:
            all_passed = False
            print("\n⚠️  Import errors:")
            for module, error in import_errors:
                print(f"   {module}: {error}")
    
    # Summary
    print("\n" + "=" * 60)
    if all_passed:
        print("✅ All checks passed! FSKB is ready to use.")
        print("\nNext steps:")
        print("  1. Run: python main.py")
        print("  2. Add a root directory via GUI")
        print("  3. Start searching!")
        print("\nSee QUICKSTART.md for a 5-minute guide.")
    else:
        print("❌ Some checks failed. Please fix the issues above.")
        print("\nCommon fixes:")
        print("  - Install dependencies: pip install -r requirements.txt")
        print("  - Ensure Python 3.9+ is installed")
        print("  - Check file permissions")
    print("=" * 60)
    
    return 0 if all_passed else 1


if __name__ == "__main__":
    sys.exit(main())

