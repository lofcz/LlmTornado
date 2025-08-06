import { defineConfig } from 'vitepress'

export default defineConfig({
  base: '/',
  title: "LlmTornado",
  description: "LlmTornado Documentation",
  appearance: 'force-dark',
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

    sidebar: [
      { text: 'Guide', link: '/getting-started' },
      { text: 'Playground', link: '/playground/', target: '_blank' },
      {
        text: 'Chat',
        items: [
          { text: 'Getting Started', link: '/getting-started' },
          { text: 'Chat Basics', link: '/chat/basics' },
          { text: 'Chat Models', link: '/chat/models' },
          { text: 'Function Calling', link: '/chat/functions' }
        ]
      }
    ]
  }
})
