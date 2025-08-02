import { defineConfig } from 'vitepress'

export default defineConfig({
  base: '/',
  title: "LlmTornado",
  description: "LlmTornado Documentation",
  appearance: 'force-dark',
  themeConfig: {
    nav: [
      { text: 'Guide', link: '/getting-started' },
      { text: 'Playground', link: '/playground/', target: '_blank' }
    ],

    sidebar: [
      { text: 'Guide', link: '/getting-started' },
      { text: 'Playground', link: '/playground/', target: '_blank' },
      {
        text: 'Guide',
        items: [
          { text: 'Getting Started', link: '/getting-started' }
        ]
      }
    ]
  }
})