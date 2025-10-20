import { defineConfig } from 'vitepress'
import { generateSidebar } from './generateSidebar.js'

export default defineConfig({
  base: '/',
  title: "LlmTornado",
  description: "LlmTornado Documentation",
  appearance: 'force-dark',
  ignoreDeadLinks: true,
  cleanUrls: true,
  rewrites: (id) => {
    // Remove numeric prefixes completely from URLs
    // Pattern: "1. LlmTornado" -> "llmtornado"
    // Pattern: "2. Agents" -> "agents"
    // Pattern: "1. basics.md" -> "basics"
    return id
      .replace(/\/(\d+)\.\s+/g, '/') // Remove numbered prefixes in paths
      .replace(/^(\d+)\.\s+/, '') // Remove numbered prefix at the beginning
      .toLowerCase()
      .replace(/\s+/g, '-')
  },
  markdown: {
    theme: {
      light: 'vitesse-light',
      dark: 'dark-plus'
    }
  },
  themeConfig: {
    nav: [
      { text: 'Guide', link: '/getting-started' },
      { text: 'Playground', link: '/playground/', target: '_blank' }
    ],

    sidebar: generateSidebar()
  }
})