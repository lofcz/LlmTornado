import { defineConfig } from 'vitepress'
import { generateSidebar } from './generateSidebar.js'

export default defineConfig({
  base: '/',
  title: "LlmTornado",
  description: "LlmTornado Documentation",
  appearance: 'force-dark',
  ignoreDeadLinks: true,
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