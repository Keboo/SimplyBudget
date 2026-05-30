import { Configuration, LogLevel } from '@azure/msal-browser'

export const msalConfig: Configuration = {
  auth: {
    clientId: '__ENTRA_CLIENT_ID__',
    authority: 'https://login.microsoftonline.com/__ENTRA_TENANT_ID__',
    redirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: 'sessionStorage',
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) return
        if (level === LogLevel.Error) console.error(message)
      },
    },
  },
}

export const loginRequest = {
  scopes: ['openid', 'profile', 'email'],
}

export const apiScopes = {
  scopes: [`api://__ENTRA_CLIENT_ID__/access_as_user`],
}
