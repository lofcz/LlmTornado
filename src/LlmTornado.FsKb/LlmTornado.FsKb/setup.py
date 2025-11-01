"""Setup script for FSKB."""

from setuptools import setup, find_packages
from pathlib import Path

# Read requirements
requirements_file = Path(__file__).parent / "requirements.txt"
with open(requirements_file, "r") as f:
    requirements = [
        line.strip()
        for line in f
        if line.strip() and not line.startswith("#")
    ]

# Read README
readme_file = Path(__file__).parent / "README.md"
with open(readme_file, "r", encoding="utf-8") as f:
    long_description = f.read()

setup(
    name="fskb",
    version="0.1.0",
    description="File System Knowledge Base - Semantic search for code repositories",
    long_description=long_description,
    long_description_content_type="text/markdown",
    author="FSKB Team",
    python_requires=">=3.9",
    packages=find_packages(),
    install_requires=requirements,
    entry_points={
        "console_scripts": [
            "fskb=main:main",
        ],
    },
    classifiers=[
        "Development Status :: 3 - Alpha",
        "Intended Audience :: Developers",
        "Topic :: Software Development :: Libraries",
        "Programming Language :: Python :: 3",
        "Programming Language :: Python :: 3.9",
        "Programming Language :: Python :: 3.10",
        "Programming Language :: Python :: 3.11",
        "Programming Language :: Python :: 3.12",
    ],
)

