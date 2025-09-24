import React from 'react'
import ReactDOM from 'react-dom/client'
import { createRouter, RouterProvider } from '@tanstack/react-router'
import * as TanstackQuery from './integrations/tanstack-query/root-provider'
import './styles.css'

// Import the generated route tree
import { routeTree } from './routeTree.gen'

const router = createRouter({
  routeTree,
  context: {
    ...TanstackQuery.getContext()
  },
  defaultPreload: 'intent'
})

// Register the router instance for type safety
declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <TanstackQuery.Provider {...TanstackQuery.getContext()}>
      <RouterProvider router={router} />
    </TanstackQuery.Provider>
  </React.StrictMode>,
)
