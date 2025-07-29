import DefaultTheme from 'vitepress/theme'
import { handleRedirect } from './redirect.js'

export default {
  ...DefaultTheme,
  enhanceApp({ app, router, siteData }) {
    handleRedirect();
  }
}