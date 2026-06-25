import { ref } from 'vue'
import { redirectToLogin } from '../services/authGateway.js'

// Shared click behaviour for every "Login" button on the landing page.
// If the login service is configured, it redirects there; otherwise it
// surfaces a friendly message instead of navigating to a broken page.
export function useLoginAction() {
  const message = ref('')

  function onLogin(role = '') {
    message.value = ''
    const redirected = redirectToLogin(role)
    if (!redirected) {
      message.value = 'Login service is being connected — please check back soon.'
    }
  }

  return { message, onLogin }
}
