/**
 * Auto-Generate Sidebar for VitePress Documentation
 * 
 * This script automatically generates the sidebar navigation from the folder structure
 * in the docs directory. No manual configuration needed!
 * 
 * How it works:
 * - Scans the docs folder recursively
 * - Excludes folders starting with "." (e.g., .vitepress)
 * - Converts folder names to section titles (e.g., "chat" → "Chat")
 * - Converts markdown filenames to page titles (e.g., "quick-start.md" → "Quick Start")
 * - Maintains natural ordering (numeric prefixes are sorted correctly: 1, 2, 11 not 1, 11, 2)
 * - Files and folders are interleaved based on their numeric prefixes
 * 
 * See README.md in this directory for more details.
 */

import { readdirSync, statSync } from 'fs'
import { join, basename, extname } from 'path'
import { fileURLToPath } from 'url'
import { dirname } from 'path'

const __filename = fileURLToPath(import.meta.url)
const __dirname = dirname(__filename)

/**
 * Convert a filename or folder name to a human-readable title
 * Examples:
 *   'getting-started' -> 'Getting Started'
 *   'chat-basics' -> 'Chat Basics'
 *   'models' -> 'Models'
 *   '1. Introduction' -> '1. Introduction'
 */
function toTitle(str) {
  // Check if the string starts with a number followed by a period (e.g., "1. ")
  const hasNumberPrefix = /^\d+\.\s/.test(str)
  
  if (hasNumberPrefix) {
    // Keep the number prefix as-is, only capitalize the rest
    return str
  }
  
  // Otherwise, apply the usual title case conversion
  return str
    .split('-')
    .map(word => word.charAt(0).toUpperCase() + word.slice(1))
    .join(' ')
}

/**
 * Natural sort comparison function that handles numeric prefixes correctly
 * Examples:
 *   '1-intro.md' < '2-basics.md' < '11-advanced.md'
 *   '1. Introduction' < '2. Getting Started' < '11. Advanced'
 *   'intro' < 'basics'
 * @param {string} a - First string to compare
 * @param {string} b - Second string to compare
 * @returns {number} -1 if a < b, 1 if a > b, 0 if equal
 */
function naturalSort(a, b) {
  // Extract numeric prefix if it exists (handles both "1-intro" and "1. Introduction" formats)
  const aMatch = a.match(/^(\d+)/)
  const bMatch = b.match(/^(\d+)/)
  
  const aNum = aMatch ? parseInt(aMatch[1], 10) : null
  const bNum = bMatch ? parseInt(bMatch[1], 10) : null
  
  // If both have numeric prefixes, compare numerically
  if (aNum !== null && bNum !== null) {
    if (aNum !== bNum) {
      return aNum - bNum
    }
    // If numbers are equal, compare the rest of the string
    return a.localeCompare(b)
  }
  
  // If only one has a numeric prefix, it comes first
  if (aNum !== null) return -1
  if (bNum !== null) return 1
  
  // If neither has a numeric prefix, use standard locale comparison
  return a.localeCompare(b)
}

/**
 * Recursively scan a directory and build sidebar structure
 * @param {string} dirPath - The directory path to scan
 * @param {string} baseDocsPath - The base docs directory path
 * @returns {Array} Array of sidebar items
 */
function buildSidebarFromDirectory(dirPath, baseDocsPath) {
  const items = []
  
  try {
    const entries = readdirSync(dirPath, { withFileTypes: true })
    
    // Separate directories and files
    const directories = entries.filter(entry => entry.isDirectory() && !entry.name.startsWith('.'))
    const files = entries.filter(entry => entry.isFile() && entry.name.endsWith('.md'))
    
    // Create a combined array with type information for proper interleaving
    const allEntries = [
      ...directories.map(d => ({ type: 'dir', name: d.name, entry: d })),
      ...files.map(f => ({ type: 'file', name: f.name, entry: f }))
    ]
    
    // Sort all entries together using natural sort
    allEntries.sort((a, b) => naturalSort(a.name, b.name))
    
    // Process entries in sorted order
    for (const item of allEntries) {
      if (item.type === 'dir') {
        const fullPath = join(dirPath, item.name)
        const nestedItems = buildSidebarFromDirectory(fullPath, baseDocsPath)
        
        if (nestedItems.length > 0) {
          items.push({
            text: toTitle(item.name),
            collapsed: false,
            items: nestedItems
          })
        }
      } else if (item.type === 'file') {
        const fileName = basename(item.name, '.md')
        
        // Skip index.md at root level (it's the home page)
        if (dirPath === baseDocsPath && fileName === 'index') {
          continue
        }
        
        // Calculate relative path from docs directory
        const relativePath = join(dirPath, item.name)
          .replace(baseDocsPath, '')
          .replace(/\\/g, '/')
          .replace(/\.md$/, '')
        
        items.push({
          text: toTitle(fileName),
          link: relativePath
        })
      }
    }
    
  } catch (error) {
    console.error(`Error reading directory ${dirPath}:`, error.message)
  }
  
  return items
}

/**
 * Generate sidebar configuration from docs folder structure
 * @returns {Array} VitePress sidebar configuration
 */
export function generateSidebar() {
  const docsPath = join(__dirname, '..')
  
  // Start with special top-level items
  const sidebar = []
  
  // Add getting-started as the first item if it exists
  sidebar.push({
    text: 'Getting Started',
    link: '/getting-started'
  })
  
  // Add playground link
  sidebar.push({
    text: 'Playground',
    link: '/playground/',
    target: '_blank'
  })
  
  // Now add the auto-generated sections from folders
  const autoGeneratedItems = buildSidebarFromDirectory(docsPath, docsPath)
  
  // Filter out the getting-started from auto-generated (we added it manually)
  const filteredItems = autoGeneratedItems.filter(item => 
    item.link !== '/getting-started'
  )
  
  sidebar.push(...filteredItems)
  
  return sidebar
}
