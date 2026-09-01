// Small indirection so non-component code (the auth store) can send the user
// back to the sign in page without importing the router and creating a cycle.
let navigateToLoginFn: (() => void) | null = null

export function setLoginNavigator(fn: () => void) {
  navigateToLoginFn = fn
}

export function navigateToLogin() {
  navigateToLoginFn?.()
}
