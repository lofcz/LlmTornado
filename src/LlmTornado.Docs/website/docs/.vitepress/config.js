import { defineConfig } from 'vitepress'

export default defineConfig({
  base: '/LlmTornado/',
  title: "LlmTornado",
  description: "LlmTornado Documentation",

  themeConfig: {
    nav: [
      { text: 'Guide', link: '/getting-started' },
      { text: 'Playground', link: '/playground/', target: '_blank' }
    ],

    sidebar: [
      {
        text: 'Guide',
        items: [
          { text: 'Getting Started', link: '/getting-started' }
        ]
      }
    ]
  }
})