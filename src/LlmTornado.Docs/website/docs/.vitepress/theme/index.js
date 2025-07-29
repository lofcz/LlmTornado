import DefaultTheme from 'vitepress/theme'
import 'aos/dist/aos.css'
import './custom.css'
import LandingPageSections from './LandingPageSections.vue';

export default {
  ...DefaultTheme,
  enhanceApp({ app }) {
    app.component('LandingPageSections', LandingPageSections);

    if (typeof window !== 'undefined') {
      import('aos').then(AOS => {
        AOS.init()
      })
    }
  }
}