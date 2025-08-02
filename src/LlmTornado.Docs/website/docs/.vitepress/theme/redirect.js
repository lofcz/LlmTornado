export function handleRedirect() {
  if (typeof window !== 'undefined') {
    const playgroundPath = '/playground/';
    if (window.location.pathname.toLowerCase().startsWith(playgroundPath) && window.location.pathname.toLowerCase() !== playgroundPath) {
      window.location.href = playgroundPath;
    }
  }
}